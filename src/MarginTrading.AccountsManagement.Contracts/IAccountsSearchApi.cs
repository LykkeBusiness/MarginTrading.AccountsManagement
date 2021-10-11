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
        /// <param name="query"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        [Get("/api/accounts/client-trading-conditions/search/byClientId")]
        Task<PaginatedResponseContract<ClientTradingConditionsSearchResultContract>> SearchByClientIdAsync(
            [Query] string query,
            [Query] int? skip = null,
            [Query] int? take = null);

        /// <summary>
        /// Search clients by account name first or account id (if name is empty) on partial matching
        /// </summary>
        /// <param name="query"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        [Get("/api/accounts/client-trading-conditions/search/byAccount")]
        Task<PaginatedResponseContract<ClientTradingConditionsSearchResultContract>> SearchByAccount(
            [Query] string query,
            [Query] int? skip = null,
            [Query] int? take = null);
    }
}