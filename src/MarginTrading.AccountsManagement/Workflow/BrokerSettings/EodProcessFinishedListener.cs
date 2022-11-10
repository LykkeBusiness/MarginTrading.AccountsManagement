// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

using BookKeeper.Client.Workflow.Events;

using Lykke.Middlewares;
using Lykke.Middlewares.Mappers;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Serializers;
using Lykke.RabbitMqBroker.Subscriber;

using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.RabbitMq;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Settings;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Internal;

namespace MarginTrading.AccountsManagement.Workflow.BrokerSettings
{
    public class EodProcessFinishedListener : HostedServiceMiddleware, IHostedService
    {
        private readonly ILossPercentageRepository _lossPercentageRepository;
        private readonly IAccountHistoryRepository _accountHistoryRepository;
        private readonly ISystemClock _systemClock;
        private readonly ILogger<EodProcessFinishedListener> _logger;
        private readonly RabbitMqSubscriber<EodProcessFinishedEvent> _eodProcessFinishedSubscriber;
        private readonly RabbitMqPublisher<LossPercentageUpdatedEvent> _lossPercentageUpdatedpublisher;
        private readonly AccountManagementSettings _settings;
        private readonly string _brokerId;

        public EodProcessFinishedListener(
            ILossPercentageRepository lossPercentageRepository,
            IAccountHistoryRepository accountHistoryRepository,
            ISystemClock systemClock,
            ILogger<EodProcessFinishedListener> logger,
            ILoggerFactory loggerFactory,
            RabbitMqSubscriber<EodProcessFinishedEvent> eodProcessFinishedSubscriber,
            AccountManagementSettings settings,
            string brokerId)
            : base(new DefaultLogLevelMapper(), logger)
        {
            _lossPercentageRepository = lossPercentageRepository;
            _accountHistoryRepository = accountHistoryRepository;
            _systemClock = systemClock;
            _logger = logger;
            _eodProcessFinishedSubscriber = eodProcessFinishedSubscriber;
            _lossPercentageUpdatedpublisher = new RabbitMqPublisher<LossPercentageUpdatedEvent>(
                    loggerFactory,
                    settings.RabbitMq.LossPercentageUpdated)
                .SetSerializer(new MessagePackMessageSerializer<LossPercentageUpdatedEvent>())
                .SetPublishStrategy(new TopicPublishingStrategy(settings.RabbitMq.LossPercentageUpdated))
                .DisableInMemoryQueuePersistence()
                .PublishSynchronously();
            _settings = settings;
            _brokerId = brokerId;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _eodProcessFinishedSubscriber
                .Subscribe(@event => this.DecorateAndHandle(() => this.CalculateLossPercentageIfNeeded()))
                .Start();
            
            _lossPercentageUpdatedpublisher.Start();

            CalculateLossPercentageIfNeeded();
            
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _eodProcessFinishedSubscriber.Stop();
            
            _lossPercentageUpdatedpublisher.Stop();

            return Task.CompletedTask;
        }

        private async Task CalculateLossPercentageIfNeeded()
        {
            var utcNow = _systemClock.UtcNow.DateTime;
            var lastLossPercentage = await _lossPercentageRepository.GetLastAsync();
            if (lastLossPercentage == null || lastLossPercentage.Timestamp < utcNow.Subtract(_settings.LossPercentageExpirationCheckPeriod))
            {
                _logger.LogInformation("Calculating loss percentage...");
                
                var calculateFrom = utcNow.Subtract(_settings.LossPercentageCalculationPeriod);
                var calculation = await _accountHistoryRepository.CalculateLossPercentageAsync(calculateFrom);

                var newLossPercentage = new LossPercentage(
                    calculation.ClientNumber,
                    calculation.LooserNumber,
                    utcNow);
                await _lossPercentageRepository.AddAsync(newLossPercentage);
                var value = newLossPercentage.LooserNumber / newLossPercentage.ClientNumber;
                
                _logger.LogInformation($"Loss percentage calculated. Value={value}.");

                try
                {
                    await _lossPercentageUpdatedpublisher.ProduceAsync(new LossPercentageUpdatedEvent
                    {
                        BrokerId = _brokerId,
                        Value = value,
                        Timestamp = utcNow
                    });
                }
                catch
                {
                    var x = 1 + 2;
                }
            }
        }
    }
}