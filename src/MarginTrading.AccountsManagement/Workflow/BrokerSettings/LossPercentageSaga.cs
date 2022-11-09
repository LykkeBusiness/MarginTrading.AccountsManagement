// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using BookKeeper.Client.Workflow.Events;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;

using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Settings;
using Microsoft.Extensions.Internal;

namespace MarginTrading.AccountsManagement.Workflow.BrokerSettings
{
    public class LossPercentageSaga
    {
        private readonly ILossPercentageRepository _lossPercentageRepository;
        private readonly IAccountHistoryRepository _accountHistoryRepository;
        private readonly CqrsContextNamesSettings _contextNames;
        private readonly ISystemClock _systemClock;
        private readonly ILog _log;
        private readonly AccountManagementSettings _settings;

        public LossPercentageSaga(
            ILossPercentageRepository lossPercentageRepository,
            IAccountHistoryRepository accountHistoryRepository,
            CqrsContextNamesSettings contextNames,
            ISystemClock systemClock,
            ILog log,
            AccountManagementSettings settings)
        {
            _lossPercentageRepository = lossPercentageRepository;
            _accountHistoryRepository = accountHistoryRepository;
            _contextNames = contextNames;
            _systemClock = systemClock;
            _log = log;
            _settings = settings;
        }

        [UsedImplicitly]
        public async Task Handle(EodProcessFinishedEvent e, ICommandSender sender)
        {
            var utcNow = _systemClock.UtcNow.DateTime;
            var lastLossPercentage = await _lossPercentageRepository.GetLastAsync();
            if (lastLossPercentage == null || lastLossPercentage.Timestamp < utcNow.Subtract(_settings.LossPercentageExpirationCheckPeriod))
            {
                await _log.WriteInfoAsync(
                    nameof(LossPercentageSaga), 
                    nameof(Handle),
                    "Calculating loss percentage...");
                
                var calculateFrom = utcNow.Subtract(_settings.LossPercentageCalculationPeriod);
                var calculation = await _accountHistoryRepository.CalculateLossPercentageAsync(calculateFrom);

                var newLossPercentage = new LossPercentage(
                    calculation.ClientNumber,
                    calculation.LooserNumber,
                    utcNow);
                await _lossPercentageRepository.AddAsync(newLossPercentage);
                var value = newLossPercentage.LooserNumber / newLossPercentage.ClientNumber;
                
                await _log.WriteInfoAsync(
                    nameof(LossPercentageSaga), 
                    nameof(Handle),
                    $"Loss percentage calculated. Value={value}.");
                
                sender.SendCommand(new UpdateLossPercentageCommand
                {
                    Value = value,
                    Timestamp = utcNow
                }, _contextNames.AccountsManagement);
            }
        }
    }
}