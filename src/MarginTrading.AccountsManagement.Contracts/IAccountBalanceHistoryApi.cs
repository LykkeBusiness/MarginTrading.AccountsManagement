﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Models;
using Refit;

namespace MarginTrading.AccountsManagement.Contracts
{
    /// <summary>
    /// Provide access to account balance history
    /// </summary>
    public interface IAccountBalanceHistoryApi
    {
        /// <summary>
        /// Get account balance change history by account Id, and optionally by dates
        /// </summary>
        [Get("/api/balance-history/")]
        Task<Dictionary<string, AccountBalanceChangeContract[]>> ByAccounts([NotNull] string[] accountIds,
            [CanBeNull] DateTime? from = null, [CanBeNull] DateTime? to = null);
    }
}