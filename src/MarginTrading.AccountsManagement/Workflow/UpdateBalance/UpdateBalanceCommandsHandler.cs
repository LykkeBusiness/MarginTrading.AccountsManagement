﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Common.Log;
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
using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Workflow.UpdateBalance.Commands;
using Microsoft.Extensions.Internal;

namespace MarginTrading.AccountsManagement.Workflow.UpdateBalance
{
    internal class UpdateBalanceCommandsHandler
    {
        private readonly INegativeProtectionService _negativeProtectionService;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IChaosKitty _chaosKitty;
        private readonly ISystemClock _systemClock;
        private readonly IConvertService _convertService;
        private readonly ILog _log;
        private readonly IOperationExecutionInfoRepository _executionInfoRepository;

        private const string OperationName = "UpdateBalance";

        public UpdateBalanceCommandsHandler(IOperationExecutionInfoRepository executionInfoRepository,
            INegativeProtectionService negativeProtectionService,
            IAccountsRepository accountsRepository,
            IChaosKitty chaosKitty, 
            ISystemClock systemClock,
            IConvertService convertService,
            ILog log)
        {
            _negativeProtectionService = negativeProtectionService;
            _accountsRepository = accountsRepository;
            _chaosKitty = chaosKitty;
            _systemClock = systemClock;
            _convertService = convertService;
            _log = log;
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
                    await _log.WriteWarningAsync(nameof(UpdateBalanceCommandsHandler),
                        nameof(UpdateBalanceInternalCommand), ex.Message);
                    
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