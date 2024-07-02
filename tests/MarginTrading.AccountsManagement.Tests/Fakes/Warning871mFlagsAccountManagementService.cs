// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

using Lykke.Snow.Common.Model;

using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.ErrorCodes;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Services;

namespace MarginTrading.AccountsManagement.Tests.Fakes
{
    internal sealed class Warning871mFlagsAccountManagementService : IAccountManagementService
    {
        public readonly IDictionary<string, bool> WarningFlags871m = new Dictionary<string, bool>();

        public Task<IAccount> CreateAsync(
            string clientId,
            string accountId,
            string tradingConditionId,
            string baseAssetId,
            string accountName,
            string userId,
            string referenceAccount)
        {
            throw new System.NotImplementedException();
        }

        public Task<IReadOnlyList<IAccount>> CreateDefaultAccountsAsync(string clientId, string tradingConditionId)
        {
            throw new System.NotImplementedException();
        }

        public Task<IReadOnlyList<IAccount>> CreateAccountsForNewBaseAssetAsync(
            string tradingConditionId,
            string baseAssetId)
        {
            throw new System.NotImplementedException();
        }

        public Task<IReadOnlyList<IAccountSuggested>> SuggestedListAsync(string query, int limit)
        {
            throw new System.NotImplementedException();
        }

        public Task<IReadOnlyList<IAccount>> ListAsync(string search, bool showDeleted = false)
        {
            throw new System.NotImplementedException();
        }

        public Task<PaginatedResponse<IAccount>> ListByPagesAsync(
            string search,
            bool showDeleted = false,
            int? skip = null,
            int? take = null,
            bool isAscendingOrder = true)
        {
            throw new System.NotImplementedException();
        }

        public Task<IReadOnlyList<IAccount>> GetByClientAsync(string clientId, bool showDeleted = false)
        {
            throw new System.NotImplementedException();
        }

        public Task<IAccount> GetByIdAsync(string accountId)
        {
            throw new System.NotImplementedException();
        }

        public ValueTask<AccountStat> GetCachedAccountStatistics(string accountId)
        {
            throw new System.NotImplementedException();
        }

        public Task<IAccount> EnsureAccountValidAsync(string accountId, bool skipDeleteValidation = false)
        {
            throw new System.NotImplementedException();
        }

        public Task<AccountCapital> GetAccountCapitalAsync(string accountId, bool useCache)
        {
            throw new System.NotImplementedException();
        }

        public Task<PaginatedResponse<IClient>> ListClientsByPagesAsync(string tradingConditionId, int skip, int take)
        {
            throw new System.NotImplementedException();
        }

        public Task<PaginatedResponse<IClientWithAccounts>> SearchByClientAsync(string query, int skip, int take)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<IClient>> GetAllClients()
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<IClientWithAccounts>> GetAllClientsWithAccounts()
        {
            throw new System.NotImplementedException();
        }

        public Task<IClient> GetClient(string clientId)
        {
            throw new System.NotImplementedException();
        }

        public Task<IAccount> UpdateAccountAsync(string accountId, bool? isDisabled, bool? isWithdrawalDisabled)
        {
            throw new System.NotImplementedException();
        }

        public Task ResetAccountAsync(string accountId)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> StartGiveTemporaryCapital(
            string eventSourceId,
            string accountId,
            decimal amount,
            string reason,
            string comment,
            string additionalInfo)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> StartRevokeTemporaryCapital(
            string eventSourceId,
            string accountId,
            string revokeEventSourceId,
            string comment,
            string additionalInfo)
        {
            throw new System.NotImplementedException();
        }

        public Task ClearStatsCache(string accountId)
        {
            throw new System.NotImplementedException();
        }

        public Task<Result<TradingConditionErrorCodes>> UpdateClientTradingCondition(
            string clientId,
            string tradingConditionId,
            string username)
        {
            throw new System.NotImplementedException();
        }

        public Task<Result<TradingConditionErrorCodes>> UpdateClientTradingConditions(
            IReadOnlyList<(string clientId, string tradingConditionId)> updates,
            string username)
        {
            throw new System.NotImplementedException();
        }

        public Task UpdateComplexityWarningFlag(
            string accountId,
            bool shouldShowProductComplexityWarning,
            string orderId = null)
        {
            throw new System.NotImplementedException();
        }

        public Task Update871mWarningFlag(string accountId, bool shouldShow871mWarning, string orderId = null)
        {
            WarningFlags871m[accountId] = shouldShow871mWarning;
            return Task.CompletedTask;
        }
    }
}