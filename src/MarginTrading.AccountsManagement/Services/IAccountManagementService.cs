// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Snow.Common.Model;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.ErrorCodes;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;

namespace MarginTrading.AccountsManagement.Services
{
    public interface IAccountManagementService
    {
        
        #region Create 
        
        Task<IAccount> CreateAsync(string clientId, string accountId, string tradingConditionId, string baseAssetId, string accountName, string userId);
        
        /// <summary>
        /// Creates default accounts for client by trading condition id.
        /// </summary>
        Task<IReadOnlyList<IAccount>> CreateDefaultAccountsAsync(string clientId, string tradingConditionId);
        
        /// <summary>
        /// Create accounts with requested base asset for all users 
        /// that already have accounts with requested trading condition
        /// </summary>
        Task<IReadOnlyList<IAccount>> CreateAccountsForNewBaseAssetAsync(string tradingConditionId, string baseAssetId);
        
        #endregion

        #region Get
        
        Task<IReadOnlyList<IAccount>> ListAsync(string search, bool showDeleted = false);

        Task<PaginatedResponse<IAccount>> ListByPagesAsync(string search, bool showDeleted = false, int? skip = null,
            int? take = null, bool isAscendingOrder = true);
        
        Task<IReadOnlyList<IAccount>> GetByClientAsync(string clientId, bool showDeleted = false);
        
        [ItemCanBeNull]
        Task<IAccount> GetByIdAsync(string accountId);

        ValueTask<AccountStat> GetCachedAccountStatistics(string accountId);

        Task<IAccount> EnsureAccountValidAsync(string accountId, bool skipDeleteValidation = false);

        Task<AccountCapital> GetAccountCapitalAsync(string accountId, bool useCache);
        
        Task<PaginatedResponse<IClient>> ListClientsByPagesAsync(string tradingConditionId, int skip, int take);
        
        Task<PaginatedResponse<IClientSearchResult>> SearchByClientAsync(string query, int skip, int take);
        
        Task<IEnumerable<IClient>> GetAllClients();

        Task<IClient> GetClient(string clientId);

        #endregion

        #region Modify

        Task<IAccount> UpdateAccountAsync(string accountId, bool? isDisabled, bool? isWithdrawalDisabled);
        
        Task ResetAccountAsync(string accountId);
        
        Task<string> StartGiveTemporaryCapital(string eventSourceId, string accountId, decimal amount, string reason,
            string comment, string additionalInfo);

        Task<string> StartRevokeTemporaryCapital(string eventSourceId, string accountId, string revokeEventSourceId,
            string comment,
            string additionalInfo);

        Task ClearStatsCache(string accountId);

        Task<Result<TradingConditionErrorCodes>> UpdateClientTradingCondition(string clientId, string tradingConditionId, string username);

        Task<Result<TradingConditionErrorCodes>> UpdateClientTradingConditions(IReadOnlyList<(string clientId, string tradingConditionId)> updates, string username);

        #endregion

        #region ComplexityWarning

        Task UpdateComplexityWarningFlag(string accountId, bool shouldShowProductComplexityWarning, string orderId = null);

        #endregion
    }
}