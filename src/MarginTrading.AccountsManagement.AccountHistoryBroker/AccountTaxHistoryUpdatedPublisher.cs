using System.Threading.Tasks;

using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Serializers;
using Lykke.RabbitMqBroker.Publisher.Strategies;

using MarginTrading.AccountsManagement.Contracts.Events;

using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.AccountHistoryBroker
{
    public class TaxHistoryInsertedPublisher : IRabbitPublisher<AccountTaxHistoryUpdatedEvent>
    {
        private RabbitMqPublisher<AccountTaxHistoryUpdatedEvent> _publisher;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Settings _settings;

        public TaxHistoryInsertedPublisher(ILoggerFactory loggerFactory, 
            Settings settings)
        {
            _loggerFactory = loggerFactory;
            _settings = settings;
        }

        public async Task PublishAsync(AccountTaxHistoryUpdatedEvent message)
        {
            await _publisher.ProduceAsync(message);
        }

        public void Start()
        {
            _publisher = new RabbitMqPublisher<AccountTaxHistoryUpdatedEvent>(
                _loggerFactory, _settings.RabbitMq.AccountTaxHistoryUpdated)
                .SetSerializer(new MessagePackMessageSerializer<AccountTaxHistoryUpdatedEvent>())
                .SetPublishStrategy(new TopicPublishStrategy(_settings.RabbitMq.AccountTaxHistoryUpdated))
                .DisableInMemoryQueuePersistence()
                .PublishSynchronously();

            _publisher.Start();
        }

        public void Dispose() 
        {
            _publisher?.Dispose();
        }

        public void Stop()
        {
            _publisher?.Dispose();
        }
    }
}
 