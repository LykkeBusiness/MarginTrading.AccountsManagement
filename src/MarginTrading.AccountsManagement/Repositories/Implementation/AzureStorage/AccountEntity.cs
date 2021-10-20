// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using MarginTrading.AccountsManagement.Contracts.Models.AdditionalInfo;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.AzureStorage
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateIfDirty)]
    public class AccountEntity : AzureTableEntity, IAccount
    {
        private decimal _withdrawTransferLimit;
        private decimal _balance;
        private bool _isDisabled;

        public string Id 
        {
            get => RowKey;
            set => RowKey = value;
        }
        
        public string ClientId
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        public string UserId { get; }

        public string TradingConditionId { get; set; }
        
        public string BaseAssetId { get; set; }
        
        public decimal Balance 
        {
            get => _balance;
            set
            {
                _balance = value;
                MarkValueTypePropertyAsDirty(nameof(Balance));
            }
        }
        
        public decimal WithdrawTransferLimit 
        {
            get => _withdrawTransferLimit;
            set
            {
                _withdrawTransferLimit = value;
                MarkValueTypePropertyAsDirty(nameof(WithdrawTransferLimit));
            }
        }
        
        public string LegalEntity { get; set; }

        public bool IsDisabled
        {
            get => _isDisabled;
            set
            {
                _isDisabled = value;
                MarkValueTypePropertyAsDirty(nameof(IsDisabled));
            }
        }

        private bool _isWithdrawalDisabled;
        public bool IsWithdrawalDisabled
        {
            get => _isWithdrawalDisabled;
            set
            {
                _isWithdrawalDisabled = value;
                MarkValueTypePropertyAsDirty(nameof(IsWithdrawalDisabled));
            }
        }


        private bool _isDeleted;
        public bool IsDeleted
        {
            get => _isDeleted;
            set
            {
                _isDeleted = value;
                MarkValueTypePropertyAsDirty(nameof(IsDeleted));
            }
        }

        public DateTime ModificationTimestamp { get; set; }


        [JsonValueSerializer]
        public List<TemporaryCapital> TemporaryCapital { get; set; } = new List<TemporaryCapital>();

        [JsonValueSerializer]
        public List<string> LastExecutedOperations { get; set; } = new List<string>();

        public string AccountName { get; set; }
        public AccountAdditionalInfo AdditionalInfo { get; }

        public static string GeneratePartitionKey(string clientId)
        {
            return clientId;
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }
    }
}