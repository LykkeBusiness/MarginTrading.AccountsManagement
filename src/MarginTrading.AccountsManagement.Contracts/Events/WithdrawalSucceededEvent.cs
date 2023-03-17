// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MessagePack;

namespace MarginTrading.AccountsManagement.Contracts.Events
{
    /// <summary>
    /// Happens when the withdrawal is completed
    /// </summary>
    [MessagePackObject]
    public class WithdrawalSucceededEvent : BaseEvent
    {
        [Key(2)] 
        public string ClientId { get; }

        [Key(3)] 
        public string AccountId { get; }

        [Key(4)] 
        public decimal Amount { get; }

        [Key(5)] 
        public string Currency { get; }

        public WithdrawalSucceededEvent([NotNull] string operationId, DateTime eventTimestamp,
            [CanBeNull] string clientId, [NotNull] string accountId, decimal amount, string currency)
            : base(operationId, eventTimestamp)
        {
            ClientId = clientId;
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            Amount = amount;
            Currency = currency;
        }
    }
}