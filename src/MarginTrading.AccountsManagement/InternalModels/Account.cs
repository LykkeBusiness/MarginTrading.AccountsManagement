﻿using System;
using System.Collections.Generic;
using AutoMapper;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Infrastructure.Implementation;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using Newtonsoft.Json;

namespace MarginTrading.AccountsManagement.InternalModels
{
    public class Account : IAccount
    {
        public Account([NotNull] string id, [NotNull] string clientId, [NotNull] string tradingConditionId, 
            [NotNull] string baseAssetId, decimal balance, decimal withdrawTransferLimit, [NotNull] string legalEntity, 
            bool isDisabled, DateTime modificationTimestamp)
        {
            Id = id.RequiredNotNullOrWhiteSpace(nameof(id));
            ClientId = clientId.RequiredNotNullOrWhiteSpace(nameof(clientId));
            TradingConditionId = tradingConditionId.RequiredNotNullOrWhiteSpace(nameof(tradingConditionId));
            BaseAssetId = baseAssetId.RequiredNotNullOrWhiteSpace(nameof(baseAssetId));
            Balance = balance;
            WithdrawTransferLimit = withdrawTransferLimit;
            LegalEntity = legalEntity.RequiredNotNullOrWhiteSpace(nameof(legalEntity));
            IsDisabled = isDisabled;
            ModificationTimestamp = modificationTimestamp;
        }

        public string Id { get; }
        public string ClientId { get; }
        public string TradingConditionId { get; }
        public string BaseAssetId { get; }
        public decimal Balance { get; }
        public decimal WithdrawTransferLimit { get; }
        public string LegalEntity { get; }
        public bool IsDisabled { get; }
        public DateTime ModificationTimestamp { get; }

        public List<string> LastExecutedOperations { get; set; } = new List<string>();
    }
}