// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Models.AdditionalInfo;
using MarginTrading.AccountsManagement.Extensions;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;

namespace MarginTrading.AccountsManagement.InternalModels
{
    public class Account : IAccount
    {
        public Account([NotNull] string id,
            [NotNull] string clientId,
            [NotNull] string tradingConditionId,
            [NotNull] string baseAssetId,
            decimal balance,
            decimal withdrawTransferLimit,
            [NotNull] string legalEntity,
            bool isDisabled,
            bool isWithdrawalDisabled,
            bool isDeleted,
            DateTime modificationTimestamp,
            string accountName,
            string userId,
            AccountAdditionalInfo additionalInfo)
        {
            Id = id.RequiredNotNullOrWhiteSpace(nameof(id));
            ClientId = clientId.RequiredNotNullOrWhiteSpace(nameof(clientId));
            UserId = userId;
            TradingConditionId = tradingConditionId.RequiredNotNullOrWhiteSpace(nameof(tradingConditionId));
            BaseAssetId = baseAssetId.RequiredNotNullOrWhiteSpace(nameof(baseAssetId));
            Balance = balance;
            WithdrawTransferLimit = withdrawTransferLimit;
            LegalEntity = legalEntity.RequiredNotNullOrWhiteSpace(nameof(legalEntity));
            IsDisabled = isDisabled;
            IsWithdrawalDisabled = isWithdrawalDisabled;
            IsDeleted = isDeleted;
            ModificationTimestamp = modificationTimestamp;
            AccountName = accountName;
            AdditionalInfo = additionalInfo ?? throw new ArgumentNullException(nameof(additionalInfo));
        }

        public string Id { get; }
        
        public string ClientId { get; }
        
        public string UserId { get; }
        
        public string TradingConditionId { get; }
        
        public string BaseAssetId { get; }
        
        public decimal Balance { get; }
        
        public decimal WithdrawTransferLimit { get; }
        
        public string LegalEntity { get; }
        
        public bool IsDisabled { get; }
        
        public bool IsWithdrawalDisabled { get; }
        
        public bool IsDeleted { get; }

        public DateTime ModificationTimestamp { get; }
        
        public DateTime ClientModificationTimestamp { get; }
        
        public List<TemporaryCapital> TemporaryCapital { get; set; } = new List<TemporaryCapital>(); 
            
        public List<string> LastExecutedOperations { get; set; } = new List<string>();

        public string AccountName { get; }
        public AccountAdditionalInfo AdditionalInfo { get; }
    }
}