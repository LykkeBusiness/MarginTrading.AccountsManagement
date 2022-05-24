// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Lykke.Snow.Common.Percents;

namespace MarginTrading.AccountsManagement.InternalModels
{
    public readonly struct AccountCapital
    {
        /// <summary>
        /// The account balance
        /// </summary>
        public decimal Balance { get; }
        
        /// <summary>
        /// The account total capital
        /// </summary>
        public decimal TotalCapital { get; }
        
        /// <summary>
        /// The temporary capital
        /// </summary>
        public decimal Temporary { get; }
        
        /// <summary>
        /// The total compensations amount
        /// </summary>
        public decimal Compensations { get; }
        
        /// <summary>
        /// The total realised PnL 
        /// </summary>
        public decimal TotalRealisedPnl { get; }
        
        /// <summary>
        /// The total unrealised PnL 
        /// </summary>
        public decimal TotalUnRealisedPnl { get; }
        
        /// <summary>
        /// The available amount
        /// </summary>
        public decimal Disposable { get; }
        
        /// <summary>
        /// The amount of temporary capital which can be revoked
        /// </summary>
        public decimal CanRevokeAmount { get; }

        /// <summary>
        /// The asset
        /// </summary>
        public string AssetId { get; }

        public AccountCapital(decimal balance,
            decimal totalCapital,
            decimal totalUnRealisedPnl,
            decimal temporary,
            decimal deals,
            decimal compensations,
            decimal dividends,
            string assetId,
            decimal usedMargin, 
            Percent disposableCapitalWithholdPercent)
        {
            if (string.IsNullOrWhiteSpace(assetId))
                throw new ArgumentNullException(nameof(assetId));
            
            Balance = balance;
            TotalCapital = totalCapital;
            Temporary = temporary;
            Compensations = compensations;
            AssetId = assetId;

            var total = deals + compensations + dividends;
            TotalRealisedPnl = total;
            TotalUnRealisedPnl = totalUnRealisedPnl;

            var balanceProtected = Math.Max(0,
                TotalCapital - (
                    Math.Max(0, Temporary) +
                    Math.Max(0, ApplyDisposableCapitalWithholdPercent(deals, disposableCapitalWithholdPercent)) +
                    Math.Max(0, ApplyDisposableCapitalWithholdPercent(compensations, disposableCapitalWithholdPercent)) +
                    Math.Max(0, ApplyDisposableCapitalWithholdPercent(dividends, disposableCapitalWithholdPercent)) +
                    Math.Max(0, ApplyDisposableCapitalWithholdPercent(totalUnRealisedPnl, disposableCapitalWithholdPercent))
                    )
                );

            Disposable = Math.Max(0, balanceProtected - usedMargin);
                    
            CanRevokeAmount = Math.Max(0,
                TotalCapital - (
                    Math.Max(0, total) + 
                    Math.Max(0, totalUnRealisedPnl)));
        }

        private static decimal ApplyDisposableCapitalWithholdPercent(decimal value, Percent percent)
        {
            return Math.Round(value * percent, 2);
        }
    }
}