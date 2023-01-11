// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Common;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Workflow.Withdrawal.Commands;
using MarginTrading.AccountsManagement.Workflow.Withdrawal.Events;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Workflow.Withdrawal
{
    internal class WithdrawalCommandsHandler
    {
        private readonly ISystemClock _systemClock;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IOperationExecutionInfoRepository _executionInfoRepository;
        private readonly IChaosKitty _chaosKitty;
        private readonly IAccountManagementService _accountManagementService;
        private readonly ILogger<WithdrawalCommandsHandler> _logger;

        private const string OperationName = "Withdraw";

        public WithdrawalCommandsHandler(
            ISystemClock systemClock,
            IAccountsRepository accountsRepository,
            IOperationExecutionInfoRepository executionInfoRepository,
            IChaosKitty chaosKitty,
            IAccountManagementService accountManagementService,
            ILogger<WithdrawalCommandsHandler> logger)
        {
            _systemClock = systemClock;
            _executionInfoRepository = executionInfoRepository;
            _accountsRepository = accountsRepository;
            _chaosKitty = chaosKitty;
            _accountManagementService = accountManagementService;
            _logger = logger;
        }

        /// <summary>
        /// Handles the command to begin the withdrawal
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(WithdrawCommand command, IEventPublisher publisher)
        {
            await _executionInfoRepository.GetOrAddAsync(
                OperationName,
                command.OperationId,
                () => new OperationExecutionInfo<WithdrawalDepositData>(
                    OperationName,
                    command.OperationId,
                    new WithdrawalDepositData
                    {
                        AccountId = command.AccountId,
                        Amount = command.Amount,
                        AuditLog = command.AuditLog,
                        State = WithdrawalState.Created,
                        Comment = command.Comment
                    },
                    _systemClock.UtcNow.UtcDateTime));

            var account = await _accountsRepository.GetAsync(command.AccountId);
            if(account == null)
            {
                _logger.LogWarning("The withdrawal couldn't be initiated. Reason: The account couldn't be found. Details: " +
                    "(OperationId: {OperationId}, AccountId: {AccountId}, Amount: {Amount})",
                    command.OperationId, command.AccountId, command.Amount);

                publisher.PublishEvent(new WithdrawalStartFailedInternalEvent(command.OperationId,
                    _systemClock.UtcNow.UtcDateTime, $"Account {command.AccountId} not found."));
                return;
            }

            var accountCapital = await _accountManagementService.GetAccountCapitalAsync(account.Id, useCache: false);
            if (accountCapital.Disposable < command.Amount)
            {
                var reasonStr = $"The account {account.Id} balance {accountCapital.Disposable}{accountCapital.AssetId} " +
                    $"is not enough to withdraw {command.Amount}{accountCapital.AssetId}. " +
                    $"Taking into account the current state of trading account: {accountCapital.ToJson()}.";

                _logger.LogWarning("The withdrawal couldn't be initiated. Reason: {ReasonStr}. Details: " +
                    "(OperationId: {OperationId}, AccountId: {AccountId}, Amount: {Amount})",
                    reasonStr, command.OperationId, command.AccountId, command.Amount);

                publisher.PublishEvent(new WithdrawalStartFailedInternalEvent(command.OperationId,
                    _systemClock.UtcNow.UtcDateTime,
                    reasonStr));
                return;
            }

            if (account.IsWithdrawalDisabled)
            {
                _logger.LogWarning("The withdrawal couldn't be initiated. Reason: {ReasonStr}. Details: " +
                    "(OperationId: {OperationId}, AccountId: {AccountId}, Amount: {Amount})",
                    "Withdrawal is disabled for the account", command.OperationId, command.AccountId, command.Amount);

                publisher.PublishEvent(new WithdrawalStartFailedInternalEvent(command.OperationId,
                    _systemClock.UtcNow.UtcDateTime, "Withdrawal is disabled"));
                return;
            }

            _chaosKitty.Meow(command.OperationId);

            _logger.LogInformation("Withdrawal initiated successfully for the account. " +
                "Details: (OperationId: {OperationId}, AccountId: {AccountId}, Amount: {Amount})",
                command.OperationId, command.AccountId, command.Amount);
          
            publisher.PublishEvent(new WithdrawalStartedInternalEvent(command.OperationId, 
                _systemClock.UtcNow.UtcDateTime));
        }

        /// <summary>
        /// Handles the command to fail the withdrawal
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(FailWithdrawalInternalCommand command, IEventPublisher publisher)
        {
            var executionInfo = await _executionInfoRepository.GetAsync<WithdrawalDepositData>(
                OperationName,
                command.OperationId
            );

            if (executionInfo == null)
                return;
            
            var account = await _accountsRepository.GetAsync(executionInfo.Data.AccountId);

            _logger.LogWarning("The withdrawal has failed. Reason: {ReasonStr}. Details: " +
                "(OperationId: {OperationId}, AccountId: {AccountId}, Amount: {Amount})",
                command.Reason, command.OperationId, executionInfo.Data.AccountId, executionInfo.Data.Amount);

            publisher.PublishEvent(new WithdrawalFailedEvent(
                command.OperationId,
                _systemClock.UtcNow.UtcDateTime, 
                command.Reason,
                executionInfo.Data.AccountId,
                account?.ClientId,
                executionInfo.Data.Amount));
        }

        /// <summary>
        /// Handles the command to complete the withdrawal
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(CompleteWithdrawalInternalCommand command, IEventPublisher publisher)
        {
            var executionInfo = await _executionInfoRepository.GetAsync<WithdrawalDepositData>(
                OperationName,
                command.OperationId
            );

            if (executionInfo == null)
                return;

            var account = await _accountsRepository.GetAsync(executionInfo.Data.AccountId);

            _logger.LogInformation("The withdrawal operation is finished successfully. Details: " +
                "(OperationId: {OperationId}, AccountId: {AccountId}, Amount: {Amount})",
                command.OperationId, account.Id, executionInfo.Data.Amount);

            publisher.PublishEvent(new WithdrawalSucceededEvent(command.OperationId, _systemClock.UtcNow.UtcDateTime,
                account?.ClientId, executionInfo.Data.AccountId, executionInfo.Data.Amount));
        }
    }
}