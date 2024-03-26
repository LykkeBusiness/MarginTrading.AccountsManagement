// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Models;
using Refit;

namespace MarginTrading.AccountsManagement.Contracts
{
    /// <summary>
    /// Provide access to account balance history
    /// </summary>
    public interface IAccountBalanceHistoryApi
    {
        /// <summary>
        /// Get balance change history by date grouped by account
        /// </summary>
        [Get("/api/balance-history/by-date")]
        Task<Dictionary<string, AccountBalanceChangeLightContract[]>> ByDate([Query] DateTime? from, [Query] DateTime? to);
        
        /// <summary>
        /// Get account balance change history paginated, by account Id, and optionally by dates and asset pair
        /// </summary>
        [Get("/api/balance-history/by-pages/{accountId}")]
        Task<ByPagesAccountBalanceHistoryResponse> ByPages([NotNull] string accountId,
            [CanBeNull] [Query] DateTime? @from = null,
            [CanBeNull] [Query] DateTime? to = null,
            [CanBeNull] [Query(CollectionFormat.Multi)] AccountBalanceChangeReasonTypeContract[] reasonTypes = null,
            [CanBeNull] [Query] string assetPairId = null,
            [CanBeNull] [Query] int? skip = null,
            [CanBeNull] [Query] int? take = null,
            [Query] bool isAscendingOrder = true);

        /// <summary>
        /// Get account balance change history by account Id, and optionally by dates
        /// </summary>
        [Get("/api/balance-history/by-account/{accountId}")]
        Task<Dictionary<string, AccountBalanceChangeContract[]>> ByAccount([NotNull] string accountId,
            [CanBeNull] [Query] DateTime? @from = null,
            [CanBeNull] [Query] DateTime? to = null,
            [CanBeNull] [Query] AccountBalanceChangeReasonTypeContract? reasonType = null,
            [Query] bool filterByTradingDay = false);
        
        /// <summary>
        /// Get account balance change history by account Id and TradingDay
        /// </summary>
        [Get("/api/balance-history/by-account/{accountId}/trading-day")]
        Task<Dictionary<string, AccountBalanceChangeContract[]>> ByAccount([NotNull] string accountId,
            [Query] DateTime tradingDay,
            [CanBeNull] [Query] AccountBalanceChangeReasonTypeContract? reasonType = null);

        /// <summary>
        /// Get account balance change history by account Id and eventSourceId (like Withdraw or Deposit)
        /// </summary>
        [Get("/api/balance-history/{accountId}")]
        Task<AccountBalanceChangeContract[]> ByAccountAndEventSource(
            [NotNull] string accountId,
            [CanBeNull][Query] string eventSourceId = null);

        /// <summary>
        /// Get account balance on a particular date
        /// </summary>
        [Get("/api/balance/{accountId}")]
        Task<decimal> GetBalanceOnDate([NotNull] string accountId, [Query] DateTime? date);
    }
}