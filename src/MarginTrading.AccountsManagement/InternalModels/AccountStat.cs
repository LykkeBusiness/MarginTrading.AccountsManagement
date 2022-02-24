// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;

namespace MarginTrading.AccountsManagement.InternalModels
{
    public class AccountStat
    {
        public decimal RealisedPnl { get; }
        
        public decimal DepositAmount { get; }
        
        public decimal WithdrawalAmount { get; }
        
        public decimal CommissionAmount { get; }
        
        public decimal OtherAmount { get; }
        
        public decimal PrevEodAccountBalance { get; }
        
        public decimal DisposableCapital { get; }
        
        public decimal UnRealisedPnl { get; }

        public decimal TotalCapital { get; }

        public decimal UsedMargin { get; }

        public decimal FreeMargin { get; }

        public decimal Pnl { get; }

        public decimal Balance { get; }

        public decimal UnrealizedPnlDaily { get; }

        public decimal CurrentlyUsedMargin { get; }

        public decimal InitiallyUsedMargin { get; }

        public int OpenPositionsCount { get; }

        public DateTime LastBalanceChangeTime { get; }

        public string AdditionalInfo { get; }

        public AccountStat(decimal realisedPnl, decimal depositAmount,
            decimal withdrawalAmount, decimal commissionAmount, decimal otherAmount,
            decimal prevEodAccountBalance, decimal disposableCapital, decimal unRealisedPnl, 
            decimal totalCapital, decimal usedMargin, 
            decimal freeMargin, decimal pnl, decimal balance, decimal unrealizedPnlDaily, decimal currentlyUsedMargin, 
            decimal initiallyUsedMargin, int openPositionsCount, DateTime lastBalanceChangeTime, string additionalInfo)
        {
            RealisedPnl = realisedPnl;
            DepositAmount = depositAmount;
            WithdrawalAmount = withdrawalAmount;
            CommissionAmount = commissionAmount;
            OtherAmount = otherAmount;
            PrevEodAccountBalance = prevEodAccountBalance;
            DisposableCapital = disposableCapital;
            UnRealisedPnl = unRealisedPnl;
            TotalCapital = totalCapital;
            UsedMargin = usedMargin;
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