// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Logging;
using Lykke.MarginTrading.BrokerBase;
using Lykke.MarginTrading.BrokerBase.Models;
using Lykke.MarginTrading.BrokerBase.Settings;
using Lykke.Snow.Common.Correlation;
using Lykke.Snow.Common.Correlation.RabbitMq;

using MarginTrading.AccountsManagement.AccountHistoryBroker.Extensions;
using MarginTrading.AccountsManagement.AccountHistoryBroker.Models;
using MarginTrading.AccountsManagement.AccountHistoryBroker.Repositories;
using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;

namespace MarginTrading.AccountsManagement.AccountHistoryBroker
{
    internal class Application : BrokerApplicationBase<AccountChangedEvent>
    {
        private readonly IAccountHistoryRepository _accountHistoryRepository;
        private readonly IAccountsApi _accountsApi;
        private readonly Settings _settings;
        private readonly ILogger _logger;
        private readonly CorrelationContextAccessor _correlationContextAccessor;
        private readonly TaxHistoryInsertedPublisher _taxHistoryInsertedPublisher;

        public Application(
            CorrelationContextAccessor correlationContextAccessor,
            RabbitMqCorrelationManager correlationManager,
            ILoggerFactory loggerFactory,
            IAccountHistoryRepository accountHistoryRepository,
            ILogger<Application> logger,
            Settings settings,
            CurrentApplicationInfo applicationInfo,
            IAccountsApi accountsApi, 
            TaxHistoryInsertedPublisher taxHistoryInsertedPublisher) : base(correlationManager, loggerFactory, logger, applicationInfo, MessageFormat.MessagePack) 
        {
            _correlationContextAccessor = correlationContextAccessor;
            _accountHistoryRepository = accountHistoryRepository;
            _logger = logger;
            _settings = settings;
            _accountsApi = accountsApi;
            _taxHistoryInsertedPublisher = taxHistoryInsertedPublisher;

            _taxHistoryInsertedPublisher.Start();
        }

        protected override BrokerSettingsBase Settings => _settings;
        protected override string ExchangeName => _settings.RabbitMqQueues.AccountHistory.ExchangeName;
        public override string RoutingKey => nameof(AccountChangedEvent);

        protected override async Task HandleMessage(AccountChangedEvent accountChangedEvent)
        {
            var correlationId = _correlationContextAccessor.CorrelationContext?.CorrelationId;
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                _logger.LogDebug("Correlation id is empty for account {AccountId}. OperationId: {OperationId}",
                    accountChangedEvent.Account.Id, accountChangedEvent.OperationId);
            }
                    
            try
            {
                if (accountChangedEvent.BalanceChange == null)
                {
                    _logger.LogInformation(
                        "No history event with BalanceChange=null is permitted to be written, eventJson=[{EventJson}]",
                        accountChangedEvent.ToJson());
                    return;
                }
                
                var accountHistory = Map(accountChangedEvent.BalanceChange, correlationId);

                if (accountHistory.ChangeAmount != 0)
                {
                    await _accountHistoryRepository.InsertAsync(accountHistory);
                
                    await InvalidateCache(accountHistory.AccountId);
                    
                    if(accountHistory.ReasonType == AccountBalanceChangeReasonType.Tax)
                    {
                        var taxHistoryUpdatedEvent = new AccountTaxHistoryUpdatedEvent(operationId: accountChangedEvent.OperationId,
                            changeTimestamp: DateTime.UtcNow, account: accountChangedEvent.Account);

                        await _taxHistoryInsertedPublisher.PublishAsync(taxHistoryUpdatedEvent);
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while processing account changed event");
                throw;
            }
        }


        private async Task InvalidateCache(string accountId)
        {
            try
            {
                await _accountsApi.RecalculateStat(accountId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while invalidating cache for account {AccountId}", accountId);
            }
        }

        private static AccountHistory Map(AccountBalanceChangeContract accountBalanceChangeContract, string correlationId)
        {
            return new AccountHistory(
                accountBalanceChangeContract.Id,
                changeAmount: accountBalanceChangeContract.ChangeAmount,
                accountId: accountBalanceChangeContract.AccountId,
                changeTimestamp: accountBalanceChangeContract.ChangeTimestamp,
                clientId: accountBalanceChangeContract.ClientId,
                balance: accountBalanceChangeContract.Balance,
                withdrawTransferLimit: accountBalanceChangeContract.WithdrawTransferLimit,
                comment: accountBalanceChangeContract.Comment,
                reasonType: accountBalanceChangeContract.ReasonType.ToType<AccountBalanceChangeReasonType>(),
                eventSourceId: accountBalanceChangeContract.EventSourceId,
                legalEntity: accountBalanceChangeContract.LegalEntity,
                auditLog: accountBalanceChangeContract.AuditLog,
                instrument: accountBalanceChangeContract.Instrument,
                tradingDate: accountBalanceChangeContract.TradingDate,
                correlationId: correlationId);
        }
    }
}