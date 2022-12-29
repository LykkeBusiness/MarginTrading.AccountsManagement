// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MarginTrading.AccountsManagement.Contracts.Models.AdditionalInfo;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using Newtonsoft.Json;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    public class AccountEntity : IAccount
    {
        public string Id { get; set; }
        
        public string ClientId { get; set; }
        
        public string TradingConditionId { get; set; }
        
        public string UserId { get; set; }
        
        public string BaseAssetId { get; set; }

        public decimal Balance { get; set; }

        public decimal WithdrawTransferLimit { get; set; }
        
        public string LegalEntity { get; set; }

        public bool IsDisabled { get; set; }
        
        public bool IsWithdrawalDisabled { get; set; }
        
        public bool IsDeleted { get; set; }

        public DateTime ModificationTimestamp { get; set; }

        List<TemporaryCapital> IAccount.TemporaryCapital => 
            DeserializeUtils.DeserializeTemporaryCapital(TemporaryCapital);
        public string TemporaryCapital { get; set; } = "[]";

        List<string> IAccount.LastExecutedOperations => JsonConvert.DeserializeObject<List<string>>(LastExecutedOperations);
        public string AccountName { get; set; }
        public string LastExecutedOperations { get; set; } = "[]";

        AccountAdditionalInfo IAccount.AdditionalInfo => JsonConvert.DeserializeObject<AccountAdditionalInfo>(AdditionalInfo);
        public string AdditionalInfo { get; set; } = "{}";
    }
}