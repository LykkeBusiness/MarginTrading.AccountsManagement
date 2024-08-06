// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using BookKeeper.Client.Workflow.Events;

using JetBrains.Annotations;

using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Snow.Common.Startup;

using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Settings;

using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.MessageHandlers
{
    [UsedImplicitly]
    internal sealed class EodFinishedHandler : IMessageHandler<EodProcessFinishedEvent>
    {
        private readonly ILossPercentageRepository _lossPercentageRepository;
        private readonly IAccountHistoryRepository _accountHistoryRepository;
        private readonly ISystemClock _systemClock;
        private readonly AccountManagementSettings _settings;
        private readonly IRabbitPublisher<AutoComputedLossPercentageUpdateEvent> _lossPercentageProducer;
        private readonly BrokerId _brokerId;
        private readonly ILogger<EodFinishedHandler> _logger;

        public EodFinishedHandler(
            ILossPercentageRepository lossPercentageRepository,
            IAccountHistoryRepository accountHistoryRepository,
            ISystemClock systemClock,
            AccountManagementSettings settings,
            IRabbitPublisher<AutoComputedLossPercentageUpdateEvent> lossPercentageProducer,
            BrokerId brokerId,
            ILogger<EodFinishedHandler> logger)
        {
            _lossPercentageRepository = lossPercentageRepository;
            _accountHistoryRepository = accountHistoryRepository;
            _systemClock = systemClock;
            _settings = settings;
            _lossPercentageProducer = lossPercentageProducer;
            _brokerId = brokerId;
            _logger = logger;
        }
        
        public async Task Handle(EodProcessFinishedEvent message)
        {
            var utcNow = _systemClock.UtcNow.DateTime;
            var lastLossPercentage = await _lossPercentageRepository.GetLastAsync();
            var expirationCheckPeriod = TimeSpan.FromDays(_settings.LossPercentageExpirationCheckPeriodInDays);
            if (lastLossPercentage == null || lastLossPercentage.Timestamp.Date <= utcNow.Subtract(expirationCheckPeriod).Date)
            {
                _logger.LogInformation("Calculating loss percentage...");
                
                var calculationPeriod = TimeSpan.FromDays(_settings.LossPercentageCalculationPeriodInDays);
                var calculateFrom = utcNow.Subtract(calculationPeriod);
                var calculation = await _accountHistoryRepository.CalculateLossPercentageAsync(calculateFrom);

                var newLossPercentage = new LossPercentage(
                    calculation.ClientNumber,
                    calculation.LooserNumber,
                    utcNow);
                await _lossPercentageRepository.AddAsync(newLossPercentage);
                var value = newLossPercentage.ClientNumber != 0
                    ? Math.Round(newLossPercentage.LooserNumber / (decimal)newLossPercentage.ClientNumber * 100, 2)
                    : 0;
                
                _logger.LogInformation($"Loss percentage calculated. Value={value}.");

                await _lossPercentageProducer.PublishAsync(new AutoComputedLossPercentageUpdateEvent
                {
                    BrokerId = _brokerId, Value = value, Timestamp = utcNow
                });
            }
        }
    }
}