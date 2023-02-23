// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.AccountsManagement.Extensions;
using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Workflow.UpdateBalance.Commands;
using Microsoft.Extensions.Internal;

namespace MarginTrading.AccountsManagement.Workflow.UpdateBalance
{
    internal class UpdateBalanceCommandsHandler
    {
        private readonly IAccountsRepository _accountsRepository;
        private readonly IChaosKitty _chaosKitty;
        private readonly ISystemClock _systemClock;
        private readonly IConvertService _convertService;
        private readonly ILogger _logger;
        private readonly IOperationExecutionInfoRepository _executionInfoRepository;

        private const string OperationName = "UpdateBalance";

        public UpdateBalanceCommandsHandler(IOperationExecutionInfoRepository executionInfoRepository,
            IAccountsRepository accountsRepository,
            IChaosKitty chaosKitty, 
            ISystemClock systemClock,
            IConvertService convertService,
            ILogger<UpdateBalanceCommandsHandler> logger)
        {
            _accountsRepository = accountsRepository;
            _chaosKitty = chaosKitty;
            _systemClock = systemClock;
            _convertService = convertService;
            _logger = logger;
            _executionInfoRepository = executionInfoRepository;
        }

        /// <summary>
        /// Handles internal command to change the balance
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(UpdateBalanceInternalCommand command,
            IEventPublisher publisher)
        {
            var executionInfo = await _executionInfoRepository.GetOrAddAsync(
                OperationName,
                command.OperationId,
                () => new OperationExecutionInfo<OperationData>(
                    OperationName,
                    command.OperationId,
                    new  OperationData { State = OperationState.Created },
                    _systemClock.UtcNow.UtcDateTime));
            
            var amountAddedOrSubtracted = Math.Abs(command.AmountDelta);

            if (SwitchState(executionInfo.Data, OperationState.Created, OperationState.Started))
            {
                IAccount account = null;
                try
                {
                    account = await _accountsRepository.UpdateBalanceAsync(
                        command.OperationId,
                        command.AccountId,
                        command.AmountDelta,
                        false);
                }
                catch (ValidationException ex)
                {
                    _logger.LogWarning(ex, "Validation error while updating balance for account {AccountId}",
                        command.AccountId);
                    
                    _logger.LogWarning("The account balance could not be updated during {Operation}. Reason: Validation error." +
                        "Details: (OperationId: {OperationId}, AccountId: {AccountId}, Amount: {Amount})",
                        command.Source, command.OperationId, command.AccountId, amountAddedOrSubtracted);

                    publisher.PublishEvent(new AccountBalanceChangeFailedEvent(command.OperationId,
                        _systemClock.UtcNow.UtcDateTime, ex.Message, command.Source));
                
                    await _executionInfoRepository.SaveAsync(executionInfo);
                    
                    return;
                }

                _chaosKitty.Meow(command.OperationId);

                var change = new AccountBalanceChangeContract(
                    command.OperationId,
                    account.ModificationTimestamp,
                    account.Id,
                    account.ClientId,
                    command.AmountDelta,
                    account.Balance,
                    account.WithdrawTransferLimit,
                    command.Comment,
                    Convert(command.ChangeReasonType),
                    command.EventSourceId,
                    account.LegalEntity,
                    command.AuditLog,
                    command.AssetPairId,
                    command.TradingDay);

                var convertedAccount = Convert(account);

                _logger.LogInformation("The account balance has been updated after {Operation}. " +
                    "Details: (OperationId: {OperationId}, AccountId: {AccountId}, Amount: {Amount}, CurrentBalance: {CurrentBalance})",
                    command.Source, command.OperationId, command.AccountId, amountAddedOrSubtracted, account.Balance);

                publisher.PublishEvent(
                    new AccountChangedEvent(
                        change.ChangeTimestamp,
                        command.Source,
                        convertedAccount,
                        AccountChangedEventTypeContract.BalanceUpdated,
                        change,
                        command.OperationId)
                );
                
                await _executionInfoRepository.SaveAsync(executionInfo);
            }
        }

        /// <summary>
        /// Handles external balance changing command
        /// </summary>
        [UsedImplicitly]
        public async Task Handle(ChangeBalanceCommand command, IEventPublisher publisher)
        {
            await Handle(new UpdateBalanceInternalCommand(
                command.OperationId,
                command.AccountId,
                command.Amount,
                command.Reason,
                command.AuditLog,
                $"{command.ReasonType.ToString()} command",
                command.ReasonType.ToType<AccountBalanceChangeReasonType>(),
                command.EventSourceId,
                command.AssetPairId,
                command.TradingDay
            ), publisher);
        }

        private AccountContract Convert(IAccount account)
        {
            return _convertService.Convert<AccountContract>(account);
        }

        private AccountBalanceChangeReasonTypeContract Convert(AccountBalanceChangeReasonType reasonType)
        {
            return _convertService.Convert<AccountBalanceChangeReasonTypeContract>(reasonType);
        }

        private static bool SwitchState(OperationData data, OperationState expectedState, OperationState nextState)
        {
            if (data.State < expectedState)
            {
                // Throws to retry and wait until the operation will be in the required state
                throw new InvalidOperationException(
                    $"Operation execution state can't be switched: {data.State} -> {nextState}. Waiting for the {expectedState} state.");
            }

            if (data.State > expectedState)
            {
                // Already in the next state, so this event can be just ignored
                return false;
            }

            data.State = nextState;

            return true;
        }
    }
}