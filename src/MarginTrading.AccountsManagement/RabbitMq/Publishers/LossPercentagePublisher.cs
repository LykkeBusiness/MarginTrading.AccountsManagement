// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using Common;

using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Serializers;

using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Settings;

using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.RabbitMq.Publishers
{
    public class LossPercentagePublisher : IRabbitPublisher<LossPercentageUpdatedEvent>
    {
        private RabbitMqPublisher<LossPercentageUpdatedEvent> _publisher;
        private readonly AccountManagementSettings _settings;
        private readonly ILogger<LossPercentageUpdatedEvent> _log;
        private readonly ILoggerFactory _loggerFactory;

        public LossPercentagePublisher(
            AccountManagementSettings settings,
            ILoggerFactory loggerFactory)
        {
            _settings = settings;
            _loggerFactory = loggerFactory;
            _log = loggerFactory.CreateLogger<LossPercentageUpdatedEvent>();
        }

        public async Task PublishAsync(LossPercentageUpdatedEvent message)
        {
            try
            {
                await _publisher.ProduceAsync(message);
            }
            catch (Exception e)
            {
                _log.LogWarning(e, "An error occurred while publishing event.", message.ToJson());
                throw;
            }
        }

        public void Start()
        {
            _publisher = new RabbitMqPublisher<LossPercentageUpdatedEvent>(
                    _loggerFactory,
                    _settings.RabbitMq.LossPercentageUpdated)
                .SetSerializer(new MessagePackMessageSerializer<LossPercentageUpdatedEvent>())
                .SetPublishStrategy(new TopicPublishingStrategy(_settings.RabbitMq.LossPercentageUpdated))
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
            _publisher?.Stop();
        }
    }
}