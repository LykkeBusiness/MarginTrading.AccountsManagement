// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;

namespace MarginTrading.AccountsManagement.InternalModels
{
    public class AccountStat
    {
        [NotNull] public string AccountId { get; }
        
        public DateTime Created { get; }
        
        public decimal RealisedPnl { get; }
        
        public decimal DepositAmount { get; }
        
        public decimal WithdrawalAmount { get; }
        
        public decimal CommissionAmount { get; }
        
        public decimal OtherAmount { get; }
        
        public decimal AccountBalance { get; }
        
        public decimal PrevEodAccountBalance { get; }
        
        public decimal DisposableCapital { get; }
        
        public decimal UnRealisedPnl { get; }
        
        public AccountCapital AccountCapitalDetails { get; }

        public decimal TotalCapital { get; }

        public decimal UsedMargin { get; }

        public decimal UsedMarginPercent { get; }

        public decimal FreeMargin { get; }

        public decimal Pnl { get; }

        public decimal Balance { get; }

        public decimal UnrealizedPnlDaily { get; }

        public decimal CurrentlyUsedMargin { get; }

        public decimal InitiallyUsedMargin { get; }

        public int OpenPositionsCount { get; }

        public DateTime LastBalanceChangeTime { get; }

        public string AdditionalInfo { get; set; }

        public AccountStat([NotNull] string accountId, DateTime created, decimal realisedPnl, decimal depositAmount,
            decimal withdrawalAmount, decimal commissionAmount, decimal otherAmount, decimal accountBalance,
            decimal prevEodAccountBalance, decimal disposableCapital, decimal unRealisedPnl, 
            AccountCapital accountCapitalDetails, decimal totalCapital, decimal usedMargin, decimal usedMarginPercent, 
            decimal freeMargin, decimal pnl, decimal balance, decimal unrealizedPnlDaily, decimal currentlyUsedMargin, 
            decimal initiallyUsedMargin, int openPositionsCount, DateTime lastBalanceChangeTime, string additionalInfo)
        {
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            Created = created;
            RealisedPnl = realisedPnl;
            DepositAmount = depositAmount;
            WithdrawalAmount = withdrawalAmount;
            CommissionAmount = commissionAmount;
            OtherAmount = otherAmount;
            AccountBalance = accountBalance;
            PrevEodAccountBalance = prevEodAccountBalance;
            DisposableCapital = disposableCapital;
            UnRealisedPnl = unRealisedPnl;
            AccountCapitalDetails = accountCapitalDetails;
            TotalCapital = totalCapital;
            UsedMargin = usedMargin;
            UsedMarginPercent = usedMarginPercent;
            FreeMargin = freeMargin;
            Pnl = pnl;
            Balance = balance;
            UnrealizedPnlDaily = unrealizedPnlDaily;
            CurrentlyUsedMargin = currentlyUsedMargin;
            InitiallyUsedMargin = initiallyUsedMargin;
            OpenPositionsCount = openPositionsCount;
            LastBalanceChangeTime = lastBalanceChangeTime;
            AdditionalInfo = additionalInfo;
        }
    }
}