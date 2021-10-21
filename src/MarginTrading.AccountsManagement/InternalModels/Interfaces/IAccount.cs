// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MarginTrading.AccountsManagement.Contracts.Models.AdditionalInfo;

namespace MarginTrading.AccountsManagement.InternalModels.Interfaces
{
    public interface IAccount
    {
        string Id { get; }
        
        string ClientId { get; }
        
        string UserId { get; }
        
        string TradingConditionId { get; }
        
        string BaseAssetId { get; }
        
        decimal Balance { get; }
        
        decimal WithdrawTransferLimit { get; }
        
        string LegalEntity { get; }
        
        bool IsDisabled { get; }
        
        bool IsWithdrawalDisabled { get; }
        
        bool IsDeleted { get; }
        
        DateTime ModificationTimestamp { get; }
        
        
        List<TemporaryCapital> TemporaryCapital { get; }
        
        
        List<string> LastExecutedOperations { get; }

        string AccountName { get; }

        public AccountAdditionalInfo AdditionalInfo { get; }
    }


}