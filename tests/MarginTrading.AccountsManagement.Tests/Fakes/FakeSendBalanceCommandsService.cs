// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Services;

namespace MarginTrading.AccountsManagement.Tests.Fakes
{
    internal sealed class FakeSendBalanceCommandsService : ISendBalanceCommandsService
    {
        public bool ChargeManuallyAsyncWasCalled { get; private set; }
        public Task<OperationId> ChargeManuallyAsync(string accountId,
            decimal amountDelta,
            OperationId operationId,
            string reason,
            string source,
            string auditLog,
            AccountBalanceChangeReasonType type,
            string eventSourceId,
            string assetPairId,
            DateTime tradingDate)
        {
            ChargeManuallyAsyncWasCalled = true;
            return Task.FromResult(operationId);
        }

        public Task<string> WithdrawAsync(string accountId, decimal amountDelta, string operationId, string reason, string auditLog)
        {
            throw new NotImplementedException();
        }

        public Task<string> DepositAsync(string accountId, decimal amountDelta, string operationId, string reason, string auditLog)
        {
            throw new NotImplementedException();
        }

        public Task<string> GiveTemporaryCapital(string eventSourceId,
            string accountId,
            decimal amount,
            string reason,
            string comment,
            string additionalInfo)
        {
            throw new NotImplementedException();
        }

        public Task<string> RevokeTemporaryCapital(string eventSourceId,
            string accountId,
            string revokeEventSourceId,
            string comment,
            string additionalInfo)
        {
            throw new NotImplementedException();
        }
    }
}