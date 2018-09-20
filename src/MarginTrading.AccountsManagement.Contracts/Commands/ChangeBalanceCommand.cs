﻿using System;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Models;
using MessagePack;

namespace MarginTrading.AccountsManagement.Contracts.Commands
{
    /// <summary>
    /// Give ability to change account balance when taking commissions, fees
    /// and all making any other balance change, except deposit, withdraw and realized PnL.
    /// </summary>
    [MessagePackObject]
    public class ChangeBalanceCommand
    {
        /// <summary>
        /// The unique id of operation.<br/>
        /// Two operations with equal type and id are considered one operation, all duplicates are skipped.<br/>
        /// It would be a nice idea to use a <see cref="Guid"/> here.<br/>
        /// </summary>
        /// <remarks>
        /// Not required. If not provided it is autogenerated.
        /// </remarks>
        [NotNull]
        [Key(0)]
        public string OperationId { get; }

        /// <summary>
        /// Property is not used internally. It is here only to not to break the contract.
        /// </summary>
        [CanBeNull]
        [Key(1)]
        public string ClientId { get; }

        /// <summary>
        /// Account Id. Must be a client's account.
        /// </summary>
        [NotNull]
        [Key(2)]
        public string AccountId { get; }

        /// <summary>
        /// The amount of money to add or deduct from the account's balance 
        /// </summary>
        [Key(3)]
        public decimal Amount { get; }

        /// <summary>
        /// Reason of balance changing.
        /// </summary>
        [Key(4)]
        public AccountBalanceChangeReasonTypeContract ReasonType { get; }
        
        /// <summary>
        /// Reason of modification.
        /// </summary>
        [Key(5)]
        public string Reason { get; }

        /// <summary>
        /// Any additional information.
        /// </summary>
        [CanBeNull]
        [Key(6)]
        public string AuditLog { get; }
        
        /// <summary>
        /// Event source ID (order, position, trade, etc).
        /// </summary>
        [CanBeNull]
        [Key(7)]
        public string EventSourceId { get; }
        
        /// <summary>
        /// Asset Pair ID (if can be found any)
        /// </summary>
        [CanBeNull]
        [Key(8)]
        public string AssetPairId { get; }
        
        /// <summary>
        /// Current trading day. If not passed current DateTime.Day is used.
        /// </summary>
        [Key(9)]
        public DateTime TradingDay { get; }

        /// <inheritdoc />
        public ChangeBalanceCommand([NotNull] string operationId, [CanBeNull] string clientId, [NotNull] string accountId,
            decimal amount, AccountBalanceChangeReasonTypeContract reasonType, [NotNull] string reason,
            [CanBeNull] string auditLog, [CanBeNull] string eventSourceId, [CanBeNull] string assetPairId, 
            DateTime tradingDay)
        {
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
            ClientId = clientId;
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            Amount = amount;
            ReasonType = reasonType;
            Reason = reason ?? throw new ArgumentNullException(nameof(reason));
            AuditLog = auditLog;
            EventSourceId = eventSourceId;
            AssetPairId = assetPairId;
            TradingDay = tradingDay == default ? DateTime.UtcNow : tradingDay;
        }
    }
}