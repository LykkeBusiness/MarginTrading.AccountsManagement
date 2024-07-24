// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    public class AccountBalanceChangeEntity : IAccountBalanceChange
    {
        public long Oid { get; set; }
        
        public string AccountId { get; set; }
        
        public DateTime ChangeTimestamp { get; set; }
        
        public string Id { get; set; }

        public string ClientId { get; set; }

        public decimal ChangeAmount { get; set; }

        public decimal Balance { get; set; }

        public decimal WithdrawTransferLimit { get; set; }

        public string Comment { get; set; }

        AccountBalanceChangeReasonType IAccountBalanceChange.ReasonType => Enum.Parse<AccountBalanceChangeReasonType>(ReasonType); 
        public string ReasonType { get; set; }

        public string EventSourceId { get; set; }

        public string LegalEntity { get; set; }

        public string AuditLog { get; set; }
        
        public string Instrument { get; set; }
        
        public DateTime TradingDate { get; set; }
    }
}