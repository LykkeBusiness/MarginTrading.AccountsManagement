// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MarginTrading.AccountsManagement.Contracts.Models.AdditionalInfo;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Repositories.Implementation.SQL;

using IAccount = MarginTrading.AccountsManagement.InternalModels.Interfaces.IAccount;

namespace MarginTrading.AccountsManagement.RecoveryTool.Services
{

    public class FakeAccountsRepository : IAccountsRepository
    {
        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(IAccount account)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<IAccountSuggested>> GetSuggestedListAsync(string query, int limit)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<IAccount>> GetAllAsync(string clientId = null,
            string search = null,
            bool showDeleted = false)
        {
            throw new NotImplementedException();
        }

        public Task<PaginatedResponse<IAccount>> GetByPagesAsync(string search = null,
            bool showDeleted = false,
            int? skip = null,
            int? take = null,
            bool isAscendingOrder = true)
        {
            throw new NotImplementedException();
        }

        public Task<PaginatedResponse<IClient>> GetClientsByPagesAsync(string tradingConditionId, int skip, int take)
        {
            throw new NotImplementedException();
        }

        public Task<PaginatedResponse<IClientWithAccounts>> SearchByClientAsync(string query, int skip, int take)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IClient>> GetClients(IEnumerable<string> clientIds)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IClient>> GetAllClients()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IClientWithAccounts>> GetAllClientsWithAccounts()
        {
            throw new NotImplementedException();
        }

        public Task<IClient> GetClient(string clientId, bool includeDeleted = false)
        {
            throw new NotImplementedException();
        }

        public Task UpdateClientTradingCondition(string clientId, string tradingConditionId)
        {
            throw new NotImplementedException();
        }

        public Task<IAccount> GetAsync(string accountId)
        {
            return Task.FromResult(new AccountEntity()
            {
                Id = "123", ClientId = "123", ModificationTimestamp = DateTime.UtcNow,
            } as IAccount);
        }

        public Task<IAccount> GetAsync(string accountId, bool includeDeleted)
        {
            throw new NotImplementedException();
        }

        public Task<(string baseAssetId, decimal temporaryCapital)> GetBaseAssetIdAndTemporaryCapitalAsync(
            string accountId)
        {
            throw new NotImplementedException();
        }

        public Task EraseAsync(string accountId)
        {
            throw new NotImplementedException();
        }

        public Task<IAccount> UpdateBalanceAsync(string operationId,
            string accountId,
            decimal amountDelta,
            bool changeLimit)
        {
            throw new NotImplementedException();
        }

        public Task<IAccount> UpdateAccountAsync(string accountId, bool? isDisabled, bool? isWithdrawalDisabled)
        {
            throw new NotImplementedException();
        }

        public Task<IAccount> UpdateAdditionalInfo(string accountId, Action<AccountAdditionalInfo> mutate)
        {
            throw new NotImplementedException();
        }

        public Task<IAccount> DeleteAsync(string accountId)
        {
            throw new NotImplementedException();
        }

        public Task<IAccount> UpdateAccountTemporaryCapitalAsync(string accountId,
            Func<string, List<TemporaryCapital>, TemporaryCapital, bool, List<TemporaryCapital>> handler,
            TemporaryCapital temporaryCapital,
            bool isAdd)
        {
            throw new NotImplementedException();
        }

        public Task<IAccount> RollbackTemporaryCapitalRevokeAsync(string accountId,
            List<TemporaryCapital> revokedTemporaryCapital)
        {
            throw new NotImplementedException();
        }
    }
}