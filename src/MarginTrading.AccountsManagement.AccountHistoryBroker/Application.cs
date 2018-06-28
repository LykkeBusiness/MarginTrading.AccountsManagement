﻿using System.Threading.Tasks;
using Common.Log;
using Lykke.SlackNotifications;
using MarginTrading.AccountsManagement.AccountHistoryBroker.Models;
using MarginTrading.AccountsManagement.AccountHistoryBroker.Repositories;
using MarginTrading.AccountsManagement.BrokerBase;
using MarginTrading.AccountsManagement.BrokerBase.Services;
using MarginTrading.AccountsManagement.BrokerBase.Settings;
using MarginTrading.AccountsManagement.Contracts.Events;

namespace MarginTrading.AccountsManagement.AccountHistoryBroker
{
    internal class Application : BrokerApplicationBase<AccountChangedEvent>
    {
        private readonly IAccountHistoryRepository _accountHistoryRepository;
        private readonly Settings _settings;
        private readonly IConvertService _convertService;

        public Application(IAccountHistoryRepository accountHistoryRepository, ILog logger,
            Settings settings, CurrentApplicationInfo applicationInfo, IConvertService convertService)
            : base(logger, null, applicationInfo)
        {
            _accountHistoryRepository = accountHistoryRepository;
            _settings = settings;
            _convertService = convertService;
        }

        protected override BrokerSettingsBase Settings => _settings;
        protected override string ExchangeName => _settings.RabbitMqQueues.AccountHistory.ExchangeName;
        protected override string RoutingKey => nameof(AccountChangedEvent);

        protected override Task HandleMessage(AccountChangedEvent accountChangedEvent)
        {
            var accountHistory = _convertService.Convert<AccountHistory>(accountChangedEvent.BalanceChange);
            
            return _accountHistoryRepository.InsertOrReplaceAsync(accountHistory);
        }
    }
}