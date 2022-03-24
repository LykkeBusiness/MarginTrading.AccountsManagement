// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;

namespace MarginTrading.AccountsManagement.Repositories
{
    public interface IAccountBalanceChangesRepository
    {
        Task<(PaginatedResponse<IAccountBalanceChange> paginatedResponse, decimal totalAmount)> GetByPagesAsync(string accountId,
            DateTime? @from = null, DateTime? to = null, AccountBalanceChangeReasonType[] reasonTypes = null, 
            string assetPairId = null, int? skip = null, int? take = null, bool isAscendingOrder = true);

        Task<IReadOnlyList<IAccountBalanceChangeLight>> GetLightAsync(DateTime? @from = null, DateTime? to = null);

        Task<IReadOnlyList<IAccountBalanceChange>> GetAsync(string accountId,
            DateTime? @from = null, DateTime? to = null, AccountBalanceChangeReasonType? reasonType = null,
            bool filterByTradingDay = false);
        
        Task<IReadOnlyList<IAccountBalanceChange>> GetAsync(string accountId, string eventSourceId);

        Task<decimal> GetCompensationsProfit(string accountId, DateTime[] days);
        Task<decimal> GetDividendsProfit(string accountId, DateTime[] days);
        
        Task AddAsync(IAccountBalanceChange change);
        
        Task<decimal> GetBalanceAsync(string accountId, DateTime date);
    }
}