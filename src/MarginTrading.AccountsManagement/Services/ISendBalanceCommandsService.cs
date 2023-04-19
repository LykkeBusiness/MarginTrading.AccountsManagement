// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using JetBrains.Annotations;

using MarginTrading.AccountsManagement.InternalModels;

namespace MarginTrading.AccountsManagement.Services
{
    public interface ISendBalanceCommandsService
    {
        Task<OperationId> ChargeManuallyAsync(string accountId, decimal amountDelta, [CanBeNull] OperationId operationId,
            string reason, string source, string auditLog, AccountBalanceChangeReasonType type, string eventSourceId,
            string assetPairId, DateTime tradingDate);

        Task<string> WithdrawAsync(string accountId, decimal amountDelta,
            string operationId, string reason, string auditLog);

        Task<string> DepositAsync(string accountId, decimal amountDelta,
            string operationId, string reason, string auditLog);

        Task<string> GiveTemporaryCapital(string eventSourceId, string accountId, decimal amount,
            string reason, string comment, string additionalInfo);

        Task<string> RevokeTemporaryCapital(string eventSourceId, string accountId, string revokeEventSourceId,
            string comment,
            string additionalInfo);
    }
}