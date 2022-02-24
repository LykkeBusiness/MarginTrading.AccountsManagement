// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.AccountsManagement.Contracts.Models
{
    public class AccountStatContract
    {
        
        /// <summary>
        /// Realised pnl for the day
        /// </summary>
        public decimal RealisedPnl { get; }
        
        /// <summary>
        /// Deposit amount for the day
        /// </summary>
        public decimal DepositAmount { get; }
        
        /// <summary>
        /// Withdrawal amount for the day
        /// </summary>
        public decimal WithdrawalAmount { get; }
        
        /// <summary>
        /// Commission amount for the day
        /// </summary>
        public decimal CommissionAmount { get; }
        
        /// <summary>
        /// Other account balance changed for the day
        /// </summary>
        public decimal OtherAmount { get; }
        
        /// <summary>
        /// Account balance at the moment of previous EOD
        /// </summary>
        public decimal PrevEodAccountBalance { get; }
        
        /// <summary>
        /// The available balance for account
        /// </summary>
        public decimal DisposableCapital { get; }
        
        /// <summary>
        /// Refers to CHARGED UnRealised pnl for the day
        /// </summary>
        public decimal UnRealisedPnl { get; }

        /// <summary>Balance + UnrealizedPnL</summary>
        public decimal TotalCapital { get; }

        /// <summary>
        /// Margin used for maintenance of positions (considering MCO rule)
        /// = Max (CurrentlyUsedMargin, InitiallyUsedMargin/2)
        /// </summary>
        public decimal UsedMargin { get; }

        /// <summary>TotalCapital - UsedMargin</summary>
        public decimal FreeMargin { get; }

        /// <summary>Unrealized PnL from MT Core</summary>
        public decimal Pnl { get; }

        /// <summary>Sum of all cash movements except for unrealized PnL</summary>
        public decimal Balance { get; }

        /// <summary>Unrealized daily PnL</summary>
        public decimal UnrealizedPnlDaily { get; }

        /// <summary>Margin used by open positions</summary>
        public decimal CurrentlyUsedMargin { get; }

        /// <summary>Margin used for initial open of existing positions</summary>
        public decimal InitiallyUsedMargin { get; }

        /// <summary>Number of opened positions</summary>
        public int OpenPositionsCount { get; }

        public DateTime LastBalanceChangeTime { get; }

        public string AdditionalInfo { get; set; }

        public AccountStatContract(decimal realisedPnl,
            decimal depositAmount, decimal withdrawalAmount, decimal commissionAmount, decimal otherAmount,
            decimal prevEodAccountBalance, decimal disposableCapital,
            decimal unRealisedPnl, decimal totalCapital, decimal usedMargin, decimal freeMargin,
            decimal pnl, decimal balance, decimal unrealizedPnlDaily, decimal currentlyUsedMargin,
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