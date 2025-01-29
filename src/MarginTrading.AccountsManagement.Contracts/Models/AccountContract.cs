// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MessagePack;

namespace MarginTrading.AccountsManagement.Contracts.Models
{
    [UsedImplicitly]
    [MessagePackObject]
    public class AccountContract
    {
        [Key(0)]
        public string Id { get; set; }

        [Key(1)]
        public string ClientId { get; set; }

        [Key(2)]
        public string TradingConditionId { get; set; }

        [Key(3)]
        public string BaseAssetId { get; set; }

        [Key(4)]
        public decimal Balance { get; set; }

        [Key(5)]
        public decimal WithdrawTransferLimit { get; set; }

        [Key(6)]
        public string LegalEntity { get; set; }

        [Key(7)]
        public bool IsDisabled { get; set; }

        [Key(8)]
        public DateTime ModificationTimestamp { get; set; }
        
        [Key(9)]
        public bool IsWithdrawalDisabled { get; set; }
        
        [Key(10)]
        public bool IsDeleted { get; set; }

        [Key(11)]
        public string AccountName { get; set; }

        [Key(12)]
        public string AdditionalInfo { get; set; }
        
        [Key(13)] 
        public decimal TemporaryCapital { get; set; }
        
        [Key(14)]
        public DateTime ClientModificationTimestamp { get; set; }
        
        [Key(15)]
        public string ReferenceAccount { get; set; }

        [Key(16)]
        public DateTime CreationTimestamp { get; set; }
    }
}