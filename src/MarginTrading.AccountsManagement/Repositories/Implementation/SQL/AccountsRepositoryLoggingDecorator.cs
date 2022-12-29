// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Common;

using MarginTrading.AccountsManagement.Contracts.Models.AdditionalInfo;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;

using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    public class AccountsRepositoryLoggingDecorator : IAccountsRepository
    {
        private readonly IAccountsRepository _decoratee;
        private readonly ILogger<AccountsRepositoryLoggingDecorator> _logger;

        public AccountsRepositoryLoggingDecorator(IAccountsRepository decoratee,
            ILogger<AccountsRepositoryLoggingDecorator> logger)
        {
            _decoratee = decoratee ?? throw new ArgumentNullException(nameof(decoratee));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Initialize()
        {
            _decoratee.Initialize();
        }

        public Task AddAsync(IAccount account)
        {
            return _decoratee.AddAsync(account);
        }

        public Task<IReadOnlyList<IAccountSuggested>> GetSuggestedListAsync(string query, int limit)
        {
            return _decoratee.GetSuggestedListAsync(query, limit);
        }

        public Task<IReadOnlyList<IAccount>> GetAllAsync(string clientId = null,
            string search = null,
            bool showDeleted = false)
        {
            return _decoratee.GetAllAsync(clientId, search, showDeleted);
        }

        public Task<PaginatedResponse<IAccount>> GetByPagesAsync(string search = null,
            bool showDeleted = false,
            int? skip = null,
            int? take = null,
            bool isAscendingOrder = true)
        {
            return _decoratee.GetByPagesAsync(search, showDeleted, skip, take, isAscendingOrder);
        }

        public Task<PaginatedResponse<IClient>> GetClientsByPagesAsync(string tradingConditionId, int skip, int take)
        {
            return _decoratee.GetClientsByPagesAsync(tradingConditionId, skip, take);
        }

        public Task<PaginatedResponse<IClientWithAccounts>> SearchByClientAsync(string query, int skip, int take)
        {
            return _decoratee.SearchByClientAsync(query, skip, take);
        }

        public async Task<IEnumerable<IClient>> GetClients(IEnumerable<string> clientIds)
        {
            var result = await _decoratee.GetClients(clientIds);

            _logger.LogInformation("{Method}:, clientIds={ClientIds} result={Result}",
                nameof(AccountsRepository.GetClients),
                string.Join(",", clientIds),
                result?.ToJson() ?? "null");

            return result;
        }

        public async Task<IEnumerable<IClient>> GetAllClients()
        {
            var result = await _decoratee.GetAllClients();

            _logger.LogInformation("{Method}: result={Result}",
                nameof(AccountsRepository.GetAllClients),
                result?.ToJson() ?? "null");

            return result;
        }

        public async Task<IEnumerable<IClientWithAccounts>> GetAllClientsWithAccounts()
        {
            var result = await _decoratee.GetAllClientsWithAccounts();

            _logger.LogInformation("{Method}: result={Result}",
                nameof(AccountsRepository.GetAllClientsWithAccounts),
                result?.ToJson() ?? "null");

            return result;
        }

        public async Task<IClient> GetClient(string clientId, bool includeDeleted = false)
        {
            var result = await _decoratee.GetClient(clientId, includeDeleted);

            _logger.LogInformation("{Method}: clientId={ClientId}, includeDeleted={IncludeDeleted}, result={Result}",
                nameof(AccountsRepository.GetClient),
                clientId,
                includeDeleted,
                result?.ToJson() ?? "null");

            return result;
        }

        public Task UpdateClientTradingCondition(string clientId, string tradingConditionId)
        {
            return _decoratee.UpdateClientTradingCondition(clientId, tradingConditionId);
        }

        public async Task<IAccount> GetAsync(string accountId)
        {
            var result = await _decoratee.GetAsync(accountId);

            _logger.LogInformation("{Method}: accountId={AccountId}, result={Result}",
                nameof(AccountsRepository.GetAsync),
                accountId,
                result?.ToJson() ?? "null");

            return result;
        }

        public async Task<IAccount> GetAsync(string accountId, bool includeDeleted)
        {
            var result = await _decoratee.GetAsync(accountId, includeDeleted);

            _logger.LogInformation(
                "{Method}: accountId={accountId}, includeDeleted={includeDeleted}, result={resultJson}",
                nameof(AccountsRepository.GetAsync),
                accountId,
                includeDeleted,
                result?.ToJson() ?? "null");

            return result;
        }

        public async Task<(string baseAssetId, decimal? temporaryCapital)> GetBaseAssetIdAndTemporaryCapitalAsync(
            string accountId)
        {
            var result = await _decoratee.GetBaseAssetIdAndTemporaryCapitalAsync(accountId);

            _logger.LogInformation(
                "{Method}: accountId={accountId}, result={resultJson}",
                nameof(AccountsRepository.GetBaseAssetIdAndTemporaryCapitalAsync),
                accountId,
                result.ToJson());

            return result;
        }

        public Task EraseAsync(string accountId)
        {
            return _decoratee.EraseAsync(accountId);
        }

        public Task<IAccount> UpdateBalanceAsync(string operationId,
            string accountId,
            decimal amountDelta,
            bool changeLimit)
        {
            return _decoratee.UpdateBalanceAsync(operationId, accountId, amountDelta, changeLimit);
        }

        public Task<IAccount> UpdateAccountAsync(string accountId, bool? isDisabled, bool? isWithdrawalDisabled)
        {
            return _decoratee.UpdateAccountAsync(accountId, isDisabled, isWithdrawalDisabled);
        }

        public Task<IAccount> UpdateAdditionalInfo(string accountId, Action<AccountAdditionalInfo> mutate)
        {
            return _decoratee.UpdateAdditionalInfo(accountId, mutate);
        }

        public Task<IAccount> DeleteAsync(string accountId)
        {
            return _decoratee.DeleteAsync(accountId);
        }

        public Task<IAccount> UpdateAccountTemporaryCapitalAsync(string accountId,
            Func<string, List<TemporaryCapital>, TemporaryCapital, bool, List<TemporaryCapital>> handler,
            TemporaryCapital temporaryCapital,
            bool isAdd)
        {
            return _decoratee.UpdateAccountTemporaryCapitalAsync(accountId, handler, temporaryCapital, isAdd);
        }

        public Task<IAccount> RollbackTemporaryCapitalRevokeAsync(string accountId,
            List<TemporaryCapital> revokedTemporaryCapital)
        {
            return _decoratee.RollbackTemporaryCapitalRevokeAsync(accountId, revokedTemporaryCapital);
        }
    }
}