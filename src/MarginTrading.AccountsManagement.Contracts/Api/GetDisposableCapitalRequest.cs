// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using JetBrains.Annotations;

namespace MarginTrading.AccountsManagement.Contracts.Api
{
    /// <summary>
    /// Get disposable capital request details
    /// </summary>
    public class GetDisposableCapitalRequest
    {
        public class AccountCapitalFigures
        {
            public decimal Balance { get; set; }
            public DateTime LastBalanceChangeTime { get; set; }
            public decimal TotalCapital { get; set; }
            public decimal FreeMargin { get; set; }
            public decimal UsedMargin { get; set; }
            public decimal CurrentlyUsedMargin { get; set; }
            public decimal InitiallyUsedMargin { get; set; }
            public decimal PnL { get; set; }
            public decimal UnrealizedDailyPnl { get; set; }
            public int OpenPositionsCount { get; set; }
            public string AdditionalInfo { get; set; }
            public decimal TodayRealizedPnL { get; set; }
            public decimal TodayUnrealizedPnL { get; set; }
            public decimal TodayDepositAmount { get; set; }
            public decimal TodayWithdrawAmount { get; set; }
            public decimal TodayCommissionAmount { get; set; }
            public decimal TodayOtherAmount { get; set; }
            public decimal TodayStartBalance { get; set; }
            public bool AccountIsDeleted { get; set; }
            public decimal UnconfirmedMargin { get; set; }
        }
        
        /// <summary>
        /// Optional capital figures for account.
        /// Can be used if caller, typically, trading core, has this information already.
        /// </summary>
        [CanBeNull] public AccountCapitalFigures CapitalFigures { get; set; }
    }
}