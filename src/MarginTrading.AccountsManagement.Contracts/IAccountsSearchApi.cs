// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Models;
using Refit;

namespace MarginTrading.AccountsManagement.Contracts
{
    /// <summary>
    /// Accounts search API
    /// </summary>
    [PublicAPI]
    public interface IAccountsSearchApi
    {
        /// <summary>
        /// Search clients by clientId on partial matching
        /// </summary>
        /// <param name="query">The search string</param>
        /// <param name="skip">How many items to skip</param>
        /// <param name="take">How many items to take</param>
        /// <returns>The list of clients with trading condition and account names matching the search criteria</returns>
        [Get("/api/accounts/client-trading-conditions/search/by-client-id")]
        Task<PaginatedResponseContract<ClientTradingConditionsSearchResultContract>> SearchByClientIdAsync(
            [Query] string query,
            [Query] int? skip = null,
            [Query] int? take = null);

        /// <summary>
        /// Search clients by account name first or account id (if name is empty) on partial matching
        /// </summary>
        /// <param name="query">The search string</param>
        /// <param name="skip">How many items to skip</param>
        /// <param name="take">How many items to take</param>
        /// <returns>The list of clients with trading condition and account names matching the search criteria</returns>
        [Get("/api/accounts/client-trading-conditions/search/by-account")]
        Task<PaginatedResponseContract<ClientTradingConditionsSearchResultContract>> SearchByAccountAsync(
            [Query] string query,
            [Query] int? skip = null,
            [Query] int? take = null);
    }
}