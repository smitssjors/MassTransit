#nullable enable
namespace MassTransit.MongoDbIntegration
{
    using System;
    using System.Threading.Tasks;
    using Clients;
    using DependencyInjection;
    using Middleware;
    using Middleware.Outbox;
    using Outbox;
    using Serialization;
    using Transports;


    public class MongoDbScopedBusContext<TBus> :
        ScopedBusContext,
        OutboxSendContext
        where TBus : class, IBus
    {
        readonly TBus _bus;
        readonly IClientFactory _clientFactory;
        readonly MongoDbCollectionContext<OutboxMessage> _collection;
        readonly Guid _outboxId;
        readonly IServiceProvider _provider;

        IPublishEndpoint? _publishEndpoint;
        IScopedClientFactory? _scopedClientFactory;
        ISendEndpointProvider? _sendEndpointProvider;

        public MongoDbScopedBusContext(TBus bus, MongoDbContext dbContext, IClientFactory clientFactory, IServiceProvider provider)
        {
            _bus = bus;
            _clientFactory = clientFactory;
            _provider = provider;

            _collection = dbContext.GetCollection<OutboxMessage>();
            _outboxId = NewId.NextGuid();
        }

        public Task AddSend<T>(SendContext<T> context)
            where T : class
        {
            return _collection.AddSend(context, SystemTextJsonMessageSerializer.Instance, outboxId: _outboxId);
        }

        public ISendEndpointProvider SendEndpointProvider
        {
            get { return _sendEndpointProvider ??= new ScopedSendEndpointProvider<IServiceProvider>(new OutboxSendEndpointProvider(this, _bus), _provider); }
        }

        public IPublishEndpoint PublishEndpoint
        {
            get
            {
                return _publishEndpoint ??= new PublishEndpoint(new ScopedPublishEndpointProvider<IServiceProvider>(
                    new OutboxPublishEndpointProvider(this, _bus), _provider));
            }
        }

        public IScopedClientFactory ClientFactory
        {
            get
            {
                return _scopedClientFactory ??=
                    new ScopedClientFactory(new ClientFactory(new ScopedClientFactoryContext<IServiceProvider>(_clientFactory, _provider)), null);
            }
        }
    }
}
