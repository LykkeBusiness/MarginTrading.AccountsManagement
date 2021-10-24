// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Lykke.AzureStorage.Tables;
using MarginTrading.AccountsManagement.AccountHistoryBroker.Models;

namespace MarginTrading.AccountsManagement.AccountHistoryBroker.Repositories.AzureRepositories
{
    internal class AccountHistoryEntity : AzureTableEntity, IAccountHistory
    {
        public string Id
        {
            get => RowKey;
            set => RowKey = value;
        }

        public string AccountId
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        public DateTime ChangeTimestamp { get; set; }
        public string ClientId { get; set; }
        public decimal ChangeAmount { get; set; }
        public decimal Balance { get; set; }
        public decimal WithdrawTransferLimit { get; set; }
        public string Comment { get; set; }
        AccountBalanceChangeReasonType IAccountHistory.ReasonType => Enum.Parse<AccountBalanceChangeReasonType>(ReasonType);
        public string ReasonType { get; set; }
        public string EventSourceId { get; set; }
        public string LegalEntity { get; set; }
        public string AuditLog { get; set; }
        public string Instrument { get; set; }
        public DateTime TradingDate { get; set; }
        public string CorrelationId { get; set; }
    }
}