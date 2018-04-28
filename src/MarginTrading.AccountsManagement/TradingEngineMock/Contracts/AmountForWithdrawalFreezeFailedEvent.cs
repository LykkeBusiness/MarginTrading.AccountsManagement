﻿using MarginTrading.AccountsManagement.Contracts.Events;

namespace MarginTrading.AccountsManagement.TradingEngineMock.Contracts
{
    public class AmountForWithdrawalFreezeFailedEvent : AccountBalanceOperationEventBase
    {
        public AmountForWithdrawalFreezeFailedEvent(string clientId, string accountId, decimal amount,
            string operationId, string reason)
            : base(clientId, accountId, amount, operationId, reason)
        {
        }
    }
}