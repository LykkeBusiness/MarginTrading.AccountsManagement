// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Models.AdditionalInfo;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;

namespace MarginTrading.AccountsManagement.Repositories
{
    public interface IAccountsRepository
    {
        void Initialize();
        
        Task AddAsync(IAccount account);

        Task<IReadOnlyList<IAccountSuggested>> GetSuggestedListAsync(string query, int limit);
        
        Task<IReadOnlyList<IAccount>> GetAllAsync(string clientId = null, string search = null,
            bool showDeleted = false);
        
        Task<PaginatedResponse<IAccount>> GetByPagesAsync(string search = null, bool showDeleted = false,
            int? skip = null, int? take = null, bool isAscendingOrder = true);

        Task<PaginatedResponse<IClient>> GetClientsByPagesAsync(string tradingConditionId, int skip, int take);
        
        Task<PaginatedResponse<IClientWithAccounts>> SearchByClientAsync(string query, int skip, int take);
        
        Task<IEnumerable<IClient>> GetClients(IEnumerable<string> clientIds);
        Task<IEnumerable<IClient>> GetAllClients();
        Task<IEnumerable<IClientWithAccounts>> GetAllClientsWithAccounts();

        Task<IClient> GetClient(string clientId, bool includeDeleted = false);

        Task UpdateClientTradingCondition(string clientId, string tradingConditionId);

        [ItemCanBeNull]
        Task<IAccount> GetAsync(string accountId);

        [ItemCanBeNull]
        Task<IAccount> GetAsync(string accountId, bool includeDeleted);
        
        Task<(string baseAssetId, decimal temporaryCapital)> GetBaseAssetIdAndTemporaryCapitalAsync(string accountId);

        Task EraseAsync(string accountId);

        /// <summary>
        /// Updates the account if the operation has not yet been executed. 
        /// </summary>
        /// <returns>
        /// Account after update
        /// </returns>
        Task<IAccount> UpdateBalanceAsync(string operationId, string accountId,
            decimal amountDelta, bool changeLimit);

        Task<IAccount> UpdateAccountAsync(string accountId, bool? isDisabled, bool? isWithdrawalDisabled);
        

        Task<IAccount> UpdateAdditionalInfo(string accountId, Action<AccountAdditionalInfo> mutate);

        Task<IAccount> DeleteAsync(string accountId);

        Task<IAccount> UpdateAccountTemporaryCapitalAsync(string accountId,
            Func<string, List<TemporaryCapital>, TemporaryCapital, bool, List<TemporaryCapital>> handler,
            TemporaryCapital temporaryCapital,
            bool isAdd);

        Task<IAccount> RollbackTemporaryCapitalRevokeAsync(string accountId, List<TemporaryCapital> revokedTemporaryCapital);
    }
}