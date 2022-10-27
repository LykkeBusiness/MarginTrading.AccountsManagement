// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Services.Implementation;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AccountsManagement.Workflow.GiveTemporaryCapital.Commands;
using MarginTrading.AccountsManagement.Workflow.GiveTemporaryCapital.Events;
using Microsoft.Extensions.Internal;

namespace MarginTrading.AccountsManagement.Workflow.GiveTemporaryCapital
{
    internal class GiveTemporaryCapitalCommandsHandler
    {
        private readonly IAccountsRepository _accountsRepository;
        private readonly ISystemClock _systemClock;
        private readonly IOperationExecutionInfoRepository _executionInfoRepository;
        private readonly IChaosKitty _chaosKitty;
        private readonly ILogger _logger;
        private readonly AccountManagementSettings _settings;
        
        private const string OperationName = GiveTemporaryCapitalSaga.OperationName;

        public GiveTemporaryCapitalCommandsHandler(
            IAccountsRepository accountsRepository,
            ISystemClock systemClock,
            IOperationExecutionInfoRepository executionInfoRepository,
            IChaosKitty chaosKitty,
            ILogger<GiveTemporaryCapitalCommandsHandler> logger,
            AccountManagementSettings settings)
        {
            _accountsRepository = accountsRepository;
            _systemClock = systemClock;
            _executionInfoRepository = executionInfoRepository;
            _chaosKitty = chaosKitty;
            _logger = logger;
            _settings = settings;
        }

        /// <summary>
        /// Handles the command to begin giving temporary capital
        /// </summary>
        [UsedImplicitly]
        public async Task Handle(StartGiveTemporaryCapitalInternalCommand c, IEventPublisher publisher)
        {
            var executionInfo = await _executionInfoRepository.GetOrAddAsync(
                OperationName,
                c.OperationId,
                () => new OperationExecutionInfo<GiveTemporaryCapitalData>(
                    OperationName,
                    c.OperationId,
                    new GiveTemporaryCapitalData 
                    {
                        State = TemporaryCapitalState.Initiated,
                        OperationId = c.OperationId,
                        AccountId = c.AccountId,
                        Amount = c.Amount,
                        Reason = c.Reason,
                        Comment = c.Comment,
                        AdditionalInfo = c.AdditionalInfo,
                    },
                    _systemClock.UtcNow.UtcDateTime));

            if (executionInfo.Data.State != TemporaryCapitalState.Initiated)
            {
                return;
            }

            _chaosKitty.Meow(c.OperationId);
            
            var account = await _accountsRepository.GetAsync(c.AccountId);

            if (account == null)
            {
                publisher.PublishEvent(new GiveTemporaryCapitalFailedEvent(c.OperationId, 
                    _systemClock.UtcNow.UtcDateTime, $"Account {c.AccountId} not found"));
                return;
            }

            if (account.TemporaryCapital.Any(x => x.Id == c.OperationId))
            {
                publisher.PublishEvent(new GiveTemporaryCapitalStartedInternalEvent(c.OperationId,
                    _systemClock.UtcNow.UtcDateTime));
                return; 
            }

            try
            {
                await _accountsRepository.UpdateAccountTemporaryCapitalAsync(c.AccountId, 
                    AccountManagementService.UpdateTemporaryCapital,
                    new TemporaryCapital
                    {
                        Id = c.OperationId,
                        Amount = c.Amount,
                    },
                    true);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to update account temporary capital");

                publisher.PublishEvent(new GiveTemporaryCapitalFailedEvent(c.OperationId, 
                    _systemClock.UtcNow.UtcDateTime, exception.Message));
                
                return;
            }
            
            _chaosKitty.Meow($"{nameof(StartGiveTemporaryCapitalInternalCommand)}: " +
                             "publisher.PublishEvent: " + c.OperationId);

            publisher.PublishEvent(new GiveTemporaryCapitalStartedInternalEvent(c.OperationId, 
                _systemClock.UtcNow.UtcDateTime));
        }

        /// <summary>
        /// Give temporary capital operation is over, finishing it
        /// </summary>
        [UsedImplicitly]
        public async Task Handle(FinishGiveTemporaryCapitalInternalCommand c, IEventPublisher publisher)
        {
            var executionInfo = await _executionInfoRepository.GetAsync<GiveTemporaryCapitalData>(OperationName, c.OperationId);

            if (executionInfo == null)
                return;

            if (!new[] {TemporaryCapitalState.ChargedOnAccount, TemporaryCapitalState.Failing}
                .Contains(executionInfo.Data.State))
            {
                throw new Exception($"{nameof(FinishGiveTemporaryCapitalInternalCommand)} have state {executionInfo.Data.State.ToString()}, but one of [{TemporaryCapitalState.ChargedOnAccount}, {TemporaryCapitalState.Failing}] was expected. Throwing to retry in {(long) _settings.Cqrs.RetryDelay.TotalMilliseconds}ms.");
            }
            
            if (executionInfo.Data.State == TemporaryCapitalState.ChargedOnAccount)
            {
                publisher.PublishEvent(new GiveTemporaryCapitalSucceededEvent(
                    c.OperationId, 
                    _systemClock.UtcNow.UtcDateTime,
                    executionInfo.Data.OperationId,
                    executionInfo.Data.AccountId,
                    executionInfo.Data.Amount,
                    executionInfo.Data.Reason,
                    executionInfo.Data.Comment,
                    executionInfo.Data.AdditionalInfo));
            }
            else if (executionInfo.Data.State == TemporaryCapitalState.Failing)
            {
                //rollback account. if exception is thrown here, it will be retried until success
                await _accountsRepository.UpdateAccountTemporaryCapitalAsync(executionInfo.Data.AccountId, 
                    AccountManagementService.UpdateTemporaryCapital,
                    new TemporaryCapital
                    {
                        Id = executionInfo.Data.OperationId,
                        Amount = executionInfo.Data.Amount,
                    },
                    false);
            
                _chaosKitty.Meow($"{nameof(FinishGiveTemporaryCapitalInternalCommand)}: " +
                                 "publisher.PublishEvent: " + c.OperationId);
                
                publisher.PublishEvent(new GiveTemporaryCapitalFailedEvent(c.OperationId, 
                    _systemClock.UtcNow.UtcDateTime, executionInfo.Data.FailReason));
            }
        }
    }
}