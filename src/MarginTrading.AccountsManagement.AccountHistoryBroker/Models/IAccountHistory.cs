// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.AccountsManagement.AccountHistoryBroker.Models
{
    public interface IAccountHistory
    {
        string Id { get; }
        DateTime ChangeTimestamp { get; }
        string AccountId { get; }
        string ClientId { get; }
        decimal ChangeAmount { get; }
        decimal Balance { get; }
        decimal WithdrawTransferLimit { get; }
        string Comment { get; }
        AccountBalanceChangeReasonType ReasonType { get; }
        string EventSourceId { get; }
        string LegalEntity { get; }
        string AuditLog { get; }
        string Instrument { get; }
        DateTime TradingDate { get; }
        string CorrelationId { get; }
    }
}
