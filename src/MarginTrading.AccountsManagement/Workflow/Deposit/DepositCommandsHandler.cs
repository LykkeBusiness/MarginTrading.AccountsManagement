// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Workflow.Deposit.Commands;
using MarginTrading.AccountsManagement.Workflow.Deposit.Events;
using Microsoft.Extensions.Internal;

namespace MarginTrading.AccountsManagement.Workflow.Deposit
{
    internal class DepositCommandsHandler
    {
        private readonly ISystemClock _systemClock;
        private readonly IOperationExecutionInfoRepository _executionInfoRepository;
        private readonly IAccountsRepository _accountsRepository;
        private const string OperationName = "Deposit";
        private readonly IChaosKitty _chaosKitty;

        public DepositCommandsHandler(
            ISystemClock systemClock, 
            IOperationExecutionInfoRepository executionInfoRepository,
            IAccountsRepository accountsRepository,
            IChaosKitty chaosKitty)
        {
            _systemClock = systemClock;
            _executionInfoRepository = executionInfoRepository;
            _accountsRepository = accountsRepository;
            _chaosKitty = chaosKitty;
        }

        /// <summary>
        /// Handles the command to begin deposit
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(DepositCommand c, IEventPublisher publisher)
        {
            await _executionInfoRepository.GetOrAddAsync(
                OperationName,
                c.OperationId,
                () => new OperationExecutionInfo<WithdrawalDepositData>(
                    OperationName,
                    c.OperationId,
                    new WithdrawalDepositData 
                    {
                        AccountId = c.AccountId,
                        Amount = c.Amount,
                        AuditLog = c.AuditLog,
                        State = WithdrawalState.Created,
                        Comment = c.Comment
                    },
                    _systemClock.UtcNow.UtcDateTime));

            _chaosKitty.Meow(c.OperationId);

            publisher.PublishEvent(new DepositStartedInternalEvent(c.OperationId, _systemClock.UtcNow.UtcDateTime));
            
        }

        /// <summary>
        /// Handles the command to freeze amount for deposit
        /// </summary>
        [UsedImplicitly]
        private void Handle(FreezeAmountForDepositInternalCommand c, IEventPublisher publisher)
        {
            // todo: Now it always succeeds. Will be used for deposit limiting.
            publisher.PublishEvent(new AmountForDepositFrozenInternalEvent(c.OperationId, _systemClock.UtcNow.UtcDateTime));
        }

        /// <summary>
        /// Handles the command to fail deposit
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(FailDepositInternalCommand c, IEventPublisher publisher)
        {
            var executionInfo = await _executionInfoRepository.GetAsync<WithdrawalDepositData>(
                OperationName, c.OperationId);
            
            if(executionInfo == null)
                return;
            
            var account = await _accountsRepository.GetAsync(executionInfo.Data.AccountId);
            
            publisher.PublishEvent(new DepositFailedEvent(
                operationId: c.OperationId, 
                eventTimestamp: _systemClock.UtcNow.UtcDateTime,
                clientId: account?.ClientId,
                accountId: executionInfo.Data.AccountId,
                amount: executionInfo.Data.Amount));
        }

        /// <summary>
        /// Handles the command to complete deposit
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(CompleteDepositInternalCommand c, IEventPublisher publisher)
        {
            var executionInfo = await _executionInfoRepository.GetAsync<WithdrawalDepositData>(
                OperationName,
                c.OperationId
            );

            if (executionInfo == null)
                return;

            var account = await _accountsRepository.GetAsync(executionInfo.Data.AccountId);

            publisher.PublishEvent(new DepositSucceededEvent(c.OperationId, _systemClock.UtcNow.UtcDateTime,
                account?.ClientId, executionInfo.Data.AccountId, executionInfo.Data.Amount));
        }
    }
}