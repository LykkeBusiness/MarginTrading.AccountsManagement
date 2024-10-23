// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using Common;

using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Serializers;
using Lykke.RabbitMqBroker.Publisher.Strategies;

using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Settings;

using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.RabbitMq.Publishers
{
    public class LossPercentagePublisher : IRabbitPublisher<AutoComputedLossPercentageUpdateEvent>
    {
        private RabbitMqPublisher<AutoComputedLossPercentageUpdateEvent> _publisher;
        private readonly AccountManagementSettings _settings;
        private readonly ILoggerFactory _loggerFactory;

        public LossPercentagePublisher(
            AccountManagementSettings settings,
            ILoggerFactory loggerFactory)
        {
            _settings = settings;
            _loggerFactory = loggerFactory;
        }

        public async Task PublishAsync(AutoComputedLossPercentageUpdateEvent message)
        {
            await _publisher.ProduceAsync(message);
        }

        public void Start()
        {
            _publisher = new RabbitMqPublisher<AutoComputedLossPercentageUpdateEvent>(
                    _loggerFactory,
                    _settings.RabbitMq.LossPercentageUpdated)
                .SetSerializer(new MessagePackMessageSerializer<AutoComputedLossPercentageUpdateEvent>(options: null))
                .SetPublishStrategy(new TopicPublishStrategy(_settings.RabbitMq.LossPercentageUpdated))
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