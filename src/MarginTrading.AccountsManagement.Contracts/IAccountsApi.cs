// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts.ErrorCodes;
using MarginTrading.AccountsManagement.Contracts.Models;
using Refit;

namespace MarginTrading.AccountsManagement.Contracts
{
    /// <summary>
    /// Manages accounts
    /// </summary>
    [PublicAPI]
    public interface IAccountsApi
    {
        /// <summary>
        /// Gets suggested accounts
        /// </summary>
        [Get("/api/accounts/suggested")]
        Task<List<AccountSuggestedContract>> SuggestedList([Query, NotNull] string query, [Query] int limit);
        
        /// <summary>
        /// Gets all accounts
        /// </summary>
        [Get("/api/accounts/")]
        Task<List<AccountContract>> List([Query] string search = null, bool showDeleted = false);
        
        /// <summary>
        /// Gets all accounts, optionally paginated. Both skip and take must be set or unset.
        /// </summary>
        [Get("/api/accounts/by-pages")]
        Task<PaginatedResponseContract<AccountContract>> ListByPages([Query] [CanBeNull] string search = null,
            [Query] [CanBeNull] int? skip = null, [Query] [CanBeNull] int? take = null, bool showDeleted = false, bool isAscendingOrder = true);

        /// <summary>
        /// Gets all accounts by <paramref name="clientId"/>
        /// </summary>
        [Get("/api/accounts/{clientId}")]
        Task<List<AccountContract>> GetByClient(string clientId, bool showDeleted = false);

        /// <summary>
        /// Gets account by clientId and accountId
        /// </summary>
        [Get("/api/accounts/{clientId}/{accountId}")]
        [Obsolete("Use GetById.")]
        [ItemCanBeNull]
        Task<AccountContract> GetByClientAndId(string clientId, string accountId);

        /// <summary>
        /// Gets account by accountId
        /// </summary>
        [Get("/api/accounts/by-id/{accountId}")]
        [ItemCanBeNull]
        Task<AccountContract> GetById(string accountId);

        /// <summary>
        /// Creates an account
        /// </summary>
        [Post("/api/accounts/{clientId}")]
        [Obsolete("Use a single-parameter Create.")]
        Task<ApiResponse<AccountContract>> Create(string clientId, [Body] CreateAccountRequestObsolete request);
        
        /// <summary>
        /// Creates an account
        /// </summary>
        [Post("/api/accounts/")]
        Task<ApiResponse<AccountContract>> Create([Body] CreateAccountRequest request);

        /// <summary>
        /// Changes an account.
        /// </summary>
        [Patch("/api/accounts/{clientId}/{accountId}")]
        [Obsolete("Use a two-parameter Change.")]
        Task<AccountContract> Change(string clientId, string accountId, [Body] ChangeAccountRequest request);

        /// <summary>
        /// Changes an account.
        /// </summary>
        [Patch("/api/accounts/{accountId}")]
        Task<AccountContract> Change(string accountId, [Body] ChangeAccountRequest request);

        /// <summary>
        /// Starts the operation of manually charging the client's account.
        /// Amount is absolute, i.e. negative value goes for charging.
        /// </summary>
        [Post("/api/accounts/{accountId}/balance")]
        Task<string> BeginChargeManually(string accountId, [Body] AccountChargeManuallyRequest request);

        /// <summary>
        /// Starts the operation of depositing funds to the client's account. Amount should be positive.
        /// </summary>
        [Post("/api/accounts/{accountId}/balance/deposit")]
        Task<string> BeginDeposit(string accountId, [Body] AccountChargeRequest request);

        /// <summary>
        /// Starts the operation of withdrawing funds to the client's account. Amount should be positive.
        /// </summary>
        [Post("/api/accounts/{accountId}/balance/withdraw")]
        [Obsolete("Use TryBeginWithdraw method (v2)")]
        Task<string> BeginWithdraw(string accountId, [Body] AccountChargeRequest request);
        
        /// <summary>
        /// Starts the operation of withdrawing funds to the client's account. Amount should be positive.
        /// </summary>
        [Post("/api/v2/accounts/{accountId}/balance/withdraw")]
        Task<WithdrawalResponse> TryBeginWithdraw(string accountId, [Body] AccountChargeRequest request);

        /// <summary>
        /// Creates default accounts for client by trading condition id.
        /// </summary>
        [Post("/api/accounts/default-accounts")]
        Task<List<AccountContract>> CreateDefaultAccounts([Body] CreateDefaultAccountsRequest request);

        /// <summary>
        /// Create accounts with requested base asset for all users 
        /// that already have accounts with requested trading condition
        /// </summary>
        [Post("/api/accounts/new-base-asset")]
        Task<List<AccountContract>> CreateAccountsForNewBaseAsset(
            [Body] CreateAccountsForBaseAssetRequest request);

        /// <summary>
        /// Reset account balance to default value (from settings)
        /// </summary>
        /// <returns></returns>
        [Post("/api/accounts/{accountId}/reset")]
        Task Reset(string accountId);

        /// <summary>
        /// Get account statistics for the current trading day
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [Get("/api/accounts/stat/{accountId}")]
        Task<AccountStatContract> GetStat(string accountId);

        /// <summary>
        /// Get account disposable capital for the current trading day
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [Get("/api/accounts/{accountId}/disposable-capital")]
        Task<decimal?> GetDisposableCapital(string accountId);

        [Post("/api/accounts/stat/{accountId}/recalculate")]
        Task RecalculateStat(string accountId);

        /// <summary>
        /// Gets client trading conditions
        /// </summary>
        [Get("/api/accounts/client-trading-conditions/{clientId}")]
        Task<ClientTradingConditionsContract> GetClientTradingConditions([NotNull] string clientId);

        /// <summary>
        /// Gets client trading conditions
        /// </summary>
        [Get("/api/accounts/client-trading-conditions")]
        Task<PaginatedResponseContract<ClientTradingConditionsContract>> ListClientsTradingConditions([Query] string tradingConditionId, [Query] int skip = 0, [Query] int take = 20);
        
        /// <summary>
        /// Gets all client trading conditions
        /// </summary>
        [Get("/api/accounts/client-trading-conditions/all")]
        Task<IEnumerable<ClientTradingConditionsContract>> GetAllClientTradingConditions();
        
        /// <summary>
        /// Gets all client trading conditions
        /// </summary>
        [Get("/api/accounts/client-trading-conditions/all-with-accounts")]
        Task<IEnumerable<ClientTradingConditionsWithAccountsContract>> GetAllClientTradingConditionsWithAccounts();

        /// <summary>
        /// Updates client trading conditions
        /// </summary>
        [Patch("/api/accounts/client-trading-conditions")]
        Task<ErrorCodeResponse<TradingConditionErrorCodesContract>> UpdateClientTradingConditions([Body] UpdateClientTradingConditionRequest request);

        /// <summary>
        /// Updates client trading conditions
        /// </summary>
        [Patch("/api/accounts/client-trading-conditions/bulk")]
        Task<ErrorCodeResponse<TradingConditionErrorCodesContract>> UpdateClientTradingConditions([Body] UpdateClientTradingConditionsBulkRequest request);
    }
}