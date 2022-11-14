// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using MarginTrading.AccountsManagement.Extensions;

using MessagePack;

namespace MarginTrading.AccountsManagement.RecoveryTool.Model
{
    [MessagePackObject]
    public class UpdateBalanceInternalCommand
    {
        [Key(0)] public string OperationId { get; }

        [Key(1)] public string AccountId { get; }

        [Key(2)] public decimal AmountDelta { get; }

        [Key(3)] public string Comment { get; }

        [Key(4)] public string AuditLog { get; }

        [Key(5)] public string Source { get; }

        [Key(6)] public AccountBalanceChangeReasonType ChangeReasonType { get; }

        /// <summary>
        /// Event source ID (order, position, trade, etc).
        /// </summary>
        [Key(7)]
        public string EventSourceId { get; }

        /// <summary>
        /// Asset Pair ID (if can be found any)
        /// </summary>
        [Key(8)]
        public string AssetPairId { get; }

        [Key(9)] public DateTime TradingDay { get; }

        public UpdateBalanceInternalCommand(string operationId,
            string accountId,
            decimal amountDelta,
            string comment,
            string auditLog,
            string source,
            AccountBalanceChangeReasonType changeReasonType,
            string eventSourceId,
            string assetPairId,
            DateTime tradingDay)
        {
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            AmountDelta = amountDelta;
            Comment = comment;
            AuditLog = auditLog;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            EventSourceId = eventSourceId;
            AssetPairId = assetPairId;
            TradingDay = tradingDay;
            ChangeReasonType = changeReasonType.RequiredEnum(nameof(changeReasonType));
        }
    }
}