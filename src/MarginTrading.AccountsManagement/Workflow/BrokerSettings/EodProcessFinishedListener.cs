// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

using BookKeeper.Client.Workflow.Events;

using Lykke.Middlewares;
using Lykke.Middlewares.Mappers;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;

using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.InternalModels;
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
        private readonly AccountManagementSettings _settings;
        private readonly IRabbitPublisher<LossPercentageUpdatedEvent> _lossPercentageProducer;
        private readonly string _brokerId;

        public EodProcessFinishedListener(
            ILossPercentageRepository lossPercentageRepository,
            IAccountHistoryRepository accountHistoryRepository,
            ISystemClock systemClock,
            ILogger<EodProcessFinishedListener> logger,
            RabbitMqSubscriber<EodProcessFinishedEvent> eodProcessFinishedSubscriber,
            AccountManagementSettings settings,
            IRabbitPublisher<LossPercentageUpdatedEvent> lossPercentageProducer,
            string brokerId)
            : base(new DefaultLogLevelMapper(), logger)
        {
            _lossPercentageRepository = lossPercentageRepository;
            _accountHistoryRepository = accountHistoryRepository;
            _systemClock = systemClock;
            _logger = logger;
            _eodProcessFinishedSubscriber = eodProcessFinishedSubscriber;
            _settings = settings;
            _lossPercentageProducer = lossPercentageProducer;
            _brokerId = brokerId;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _eodProcessFinishedSubscriber
                .Subscribe(@event => this.DecorateAndHandle(() => this.CalculateLossPercentageIfNeeded()))
                .Start();

            CalculateLossPercentageIfNeeded();
            
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _eodProcessFinishedSubscriber.Stop();
            
            return Task.CompletedTask;
        }

        private async Task CalculateLossPercentageIfNeeded()
        {
            if (!_settings.LossPercentageCalculationEnabled)
            {
                return;
            }

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

                await _lossPercentageProducer.PublishAsync(new LossPercentageUpdatedEvent
                {
                    BrokerId = _brokerId, Value = value, Timestamp = utcNow
                });
            }
        }
    }
}