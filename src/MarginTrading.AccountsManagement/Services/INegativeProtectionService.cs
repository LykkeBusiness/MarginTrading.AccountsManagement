// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace MarginTrading.AccountsManagement.Services
{
    /// <summary>
    /// Negative account balance protection (ESMA, NBP rule)
    /// </summary>
    public interface INegativeProtectionService
    {
        /// <summary>
        /// If a portfolio of a client is negative after closing all positions, then ChargeManually is called
        /// to credit the corresponding amount to the account to cover loss.
        /// Charging is called with a special type "CompensationPayments", event is logged in a common way.
        /// </summary>
        /// <param name="operationId">The operation identifier</param>
        /// <param name="accountId">The account identifier</param>
        /// <param name="newBalance">The new balance value</param>
        /// <param name="changeAmount">The balance change amount led to new balance value</param>
        /// <returns></returns>
        Task<decimal?> CheckAsync(string operationId, string accountId, decimal newBalance, decimal changeAmount);
    }
}