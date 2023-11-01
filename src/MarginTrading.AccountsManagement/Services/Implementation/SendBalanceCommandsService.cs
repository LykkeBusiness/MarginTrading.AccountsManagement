// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AccountsManagement.Workflow.GiveTemporaryCapital.Commands;
using MarginTrading.AccountsManagement.Workflow.RevokeTemporaryCapital.Commands;
using MarginTrading.AccountsManagement.Workflow.UpdateBalance.Commands;

namespace MarginTrading.AccountsManagement.Services.Implementation
{
    public class SendBalanceCommandsService : ISendBalanceCommandsService
    {
        public ICqrsEngine _cqrsEngine { get; set; }//property injection.. to workaround circular dependency
        private readonly CqrsContextNamesSettings _cqrsContextNamesSettings;

        public SendBalanceCommandsService(CqrsContextNamesSettings cqrsContextNamesSettings)
        {
            _cqrsContextNamesSettings = cqrsContextNamesSettings;
        }

        public Task<OperationId> ChargeManuallyAsync(string accountId, decimal amountDelta,
            OperationId operationId, string reason, string source, string auditLog,
            AccountBalanceChangeReasonType type, string eventSourceId, string assetPairId, DateTime tradingDay)
        {
            operationId ??= new OperationId();
            
            _cqrsEngine.SendCommand(
                new UpdateBalanceInternalCommand(
                    operationId,
                    accountId,
                    amountDelta,
                    reason,
                    auditLog,
                    source,
                    type,
                    eventSourceId,
                    assetPairId,
                    tradingDay),
                _cqrsContextNamesSettings.AccountsManagement,
                _cqrsContextNamesSettings.AccountsManagement);
            
            return Task.FromResult(operationId);
        }

        public Task<string> WithdrawAsync(string accountId, decimal amountDelta,
            [CanBeNull] string operationId, string reason, string auditLog)
        {
            operationId ??= new OperationId();
            
            _cqrsEngine.SendCommand(
                new WithdrawCommand(
                    operationId,
                    null,
                    accountId,
                    amountDelta,
                    reason,
                    auditLog),
                _cqrsContextNamesSettings.AccountsManagement,
                _cqrsContextNamesSettings.AccountsManagement);
            
            return Task.FromResult(operationId);
        }

        public Task<string> DepositAsync(string accountId, decimal amountDelta,
            [CanBeNull] string operationId, string reason, string auditLog)
        {
            operationId ??= new OperationId();
            
            _cqrsEngine.SendCommand(
                new DepositCommand(
                    operationId,
                    null,
                    accountId,
                    amountDelta,
                    reason,
                    auditLog),
                _cqrsContextNamesSettings.AccountsManagement,
                _cqrsContextNamesSettings.AccountsManagement);
            
            return Task.FromResult(operationId);
        }

        public Task<string> GiveTemporaryCapital(string eventSourceId, string accountId, decimal amount,
            string reason, string comment, string additionalInfo)
        {
            _cqrsEngine.SendCommand(
                new StartGiveTemporaryCapitalInternalCommand(
                    eventSourceId,
                    accountId,
                    amount,
                    reason,
                    comment,
                    additionalInfo
                ),
                _cqrsContextNamesSettings.AccountsManagement,
                _cqrsContextNamesSettings.AccountsManagement);
            return Task.FromResult(eventSourceId);
        }

        public Task<string> RevokeTemporaryCapital(string eventSourceId, string accountId,
            string revokeEventSourceId, string comment, string additionalInfo)
        {
            _cqrsEngine.SendCommand(
                new StartRevokeTemporaryCapitalInternalCommand(
                    eventSourceId,
                    accountId,
                    revokeEventSourceId,
                    comment,
                    additionalInfo
                ),
                _cqrsContextNamesSettings.AccountsManagement,
                _cqrsContextNamesSettings.AccountsManagement);
            return Task.FromResult(eventSourceId);
        }
    }
}