// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AccountsManagement.Workflow.UpdateBalance.Commands;
using MarginTrading.AccountsManagement.Workflow.Withdrawal.Commands;
using MarginTrading.AccountsManagement.Workflow.Withdrawal.Events;
using Microsoft.Extensions.Internal;

namespace MarginTrading.AccountsManagement.Workflow.Withdrawal
{
    internal class WithdrawalSaga
    {
        private readonly IOperationExecutionInfoRepository _executionInfoRepository;
        private readonly CqrsContextNamesSettings _contextNames;
        private const string OperationName = "Withdraw";
        private readonly IChaosKitty _chaosKitty;
        private readonly ISystemClock _systemClock;

        public WithdrawalSaga(CqrsContextNamesSettings contextNames,
            IOperationExecutionInfoRepository executionInfoRepository,
            ISystemClock systemClock,
            IChaosKitty chaosKitty)
        {
            _contextNames = contextNames;
            _executionInfoRepository = executionInfoRepository;
            _systemClock = systemClock;
            _chaosKitty = chaosKitty;
        }

        /// <summary>
        /// The withdrawal has started => ask the backend to freeze the amount in the margin.
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(WithdrawalStartedInternalEvent e, ICommandSender sender)
        {
            var executionInfo = await _executionInfoRepository.GetAsync<WithdrawalDepositData>(OperationName, e.OperationId);

            if (executionInfo == null)
                return;

            if (SwitchState(executionInfo.Data, WithdrawalState.Created, WithdrawalState.FreezingAmount))
            {
                sender.SendCommand(
                    new FreezeAmountForWithdrawalCommand(
                        executionInfo.Id,
                        _systemClock.UtcNow.UtcDateTime,
                        executionInfo.Data.AccountId,
                        executionInfo.Data.Amount,
                        executionInfo.Data.Comment),
                    _contextNames.TradingEngine);
                
                _chaosKitty.Meow(e.OperationId);
                
                await _executionInfoRepository.SaveAsync(executionInfo);
            }
        }

        /// <summary>
        /// The backend frozen the amount in the margin => update the balance
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(AmountForWithdrawalFrozenEvent e, ICommandSender sender)
        {
            var executionInfo = await _executionInfoRepository.GetAsync<WithdrawalDepositData>(OperationName, e.OperationId);

            if (executionInfo == null)
                return;

            if (SwitchState(executionInfo.Data, WithdrawalState.FreezingAmount, WithdrawalState.UpdatingBalance))
            {
                sender.SendCommand(
                    new UpdateBalanceInternalCommand(
                        e.OperationId,
                        executionInfo.Data.AccountId,
                        -executionInfo.Data.Amount,
                        executionInfo.Data.Comment,
                        executionInfo.Data.AuditLog,
                        OperationName,
                        AccountBalanceChangeReasonType.Withdraw,
                        e.OperationId,
                        string.Empty,
                        DateTime.UtcNow),
                    _contextNames.AccountsManagement);
                
                _chaosKitty.Meow(e.OperationId);

                await _executionInfoRepository.SaveAsync(executionInfo);
            }
        }

        /// <summary>
        /// The backend failed to freeze the amount in the margin => fail the withdrawal
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(AmountForWithdrawalFreezeFailedEvent e, ICommandSender sender)
        {
            var executionInfo = await _executionInfoRepository.GetAsync<WithdrawalDepositData>(OperationName, e.OperationId);

            if (executionInfo == null)
                return;

            if (SwitchState(executionInfo.Data, WithdrawalState.FreezingAmount, WithdrawalState.Failed))
            {
                executionInfo.Data.FailReason = e.Reason;
                sender.SendCommand(
                    new FailWithdrawalInternalCommand(e.OperationId, e.Reason), 
                    _contextNames.AccountsManagement);
                
                _chaosKitty.Meow(e.OperationId);
                
                await _executionInfoRepository.SaveAsync(executionInfo);
            }
        }

        /// <summary>
        /// The balance has changed => process succeeded
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(AccountChangedEvent e, ICommandSender sender)
        {
            if (e.Source != OperationName)
                return;
            
            var executionInfo = await _executionInfoRepository.GetAsync<WithdrawalDepositData>(OperationName, e.BalanceChange.Id);

            if (executionInfo == null)
                return;

            if (SwitchState(executionInfo.Data, WithdrawalState.UpdatingBalance, WithdrawalState.Succeeded))
            {
                sender.SendCommand(
                    new CompleteWithdrawalInternalCommand(
                        e.BalanceChange.Id), 
                    _contextNames.AccountsManagement);
                
                _chaosKitty.Meow(e.BalanceChange.Id);
                
                await _executionInfoRepository.SaveAsync(executionInfo);
            }
        }

        /// <summary>
        /// Notify TradingCode that withdrawal has failed to unfreeze the margin
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(AccountBalanceChangeFailedEvent e, ICommandSender sender)
        {
            if (e.Source != OperationName)
                return;
            
            var executionInfo = await _executionInfoRepository.GetAsync<WithdrawalDepositData>(OperationName, e.OperationId);

            if (executionInfo == null)
                return;
            
            if (SwitchState(executionInfo.Data, WithdrawalState.UpdatingBalance, WithdrawalState.UnfreezingAmount))
            {
                executionInfo.Data.FailReason = e.Reason;
                sender.SendCommand(
                    new UnfreezeMarginOnFailWithdrawalCommand(
                        e.OperationId), 
                    _contextNames.TradingEngine);
                
                _chaosKitty.Meow(e.OperationId);
                
                await _executionInfoRepository.SaveAsync(executionInfo);
            }
        }

        /// <summary>
        /// Failed to change balance => withdrawal failed
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(UnfreezeMarginOnFailSucceededWithdrawalEvent e, ICommandSender sender)
        {
            var executionInfo = await _executionInfoRepository.GetAsync<WithdrawalDepositData>(OperationName, e.OperationId);

            if (executionInfo == null)
                return;

            if (SwitchState(executionInfo.Data, WithdrawalState.UnfreezingAmount, WithdrawalState.Failed))
            {
                sender.SendCommand(
                    new FailWithdrawalInternalCommand(e.OperationId, executionInfo.Data.FailReason), 
                    _contextNames.AccountsManagement);
                
                _chaosKitty.Meow(e.OperationId);
                
                await _executionInfoRepository.SaveAsync(executionInfo);
            } 
        }

        /// <summary>
        /// Balance check failed => withdrawal failed
        /// </summary>
        [UsedImplicitly]
        private Task Handle(WithdrawalStartFailedInternalEvent e, ICommandSender sender)
        {
            //there's no operation state at that point, so just failing the process.
            sender.SendCommand(new FailWithdrawalInternalCommand(e.OperationId, e.Reason), 
                _contextNames.AccountsManagement);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Withdrawal failed
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(WithdrawalFailedEvent e, ICommandSender sender)
        {
            var executionInfo = await _executionInfoRepository.GetAsync<WithdrawalDepositData>(OperationName, e.OperationId);
            
            if (executionInfo != null && SwitchState(executionInfo.Data, executionInfo.Data.State, WithdrawalState.Failed))
            {
                executionInfo.Data.FailReason = e.Reason;
                
                await _executionInfoRepository.SaveAsync(executionInfo);
            }
        }
        
        /// <summary>
        /// Withdrawal succeeded
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(WithdrawalSucceededEvent e, ICommandSender sender)
        {
            var executionInfo = await _executionInfoRepository.GetAsync<WithdrawalDepositData>(OperationName, e.OperationId);
            if (executionInfo != null && SwitchState(executionInfo.Data, executionInfo.Data.State, WithdrawalState.Succeeded))
            {
                await _executionInfoRepository.SaveAsync(executionInfo);
            }
        }

        private static bool SwitchState(WithdrawalDepositData data, WithdrawalState expectedState, WithdrawalState nextState)
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