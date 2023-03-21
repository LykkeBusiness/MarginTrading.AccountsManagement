// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MessagePack;

namespace MarginTrading.AccountsManagement.Contracts.Events
{
    /// <summary>
    /// Withdrawal operation failed
    /// </summary>
    [MessagePackObject]
    public class WithdrawalFailedEvent : BaseEvent
    {
        [Key(2)] 
        public string Reason { get; }
        
        [Key(3)]
        public string AccountId { get; }
        
        [Key(4)]
        public string ClientId { get; }
        
        [Key(5)]
        public decimal Amount { get; }

        [Key(6)]
        public string Currency { get; }

        public WithdrawalFailedEvent([NotNull] string operationId, DateTime eventTimestamp, string reason,
            string accountId, string clientId, decimal amount, string currency)
            : base(operationId, eventTimestamp)
        {
            Reason = reason;
            AccountId = accountId;
            ClientId = clientId;
            Amount = amount;
            Currency = currency;
        }
    }
}