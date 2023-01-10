// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using JetBrains.Annotations;

using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Services;

namespace MarginTrading.AccountsManagement.Extensions
{
    internal static class AccountManagementServiceExtensions
    {
        [ItemCanBeNull]
        public static async Task<AccountStat> TryGetCachedAccountStatistics(this IAccountManagementService service,
            string accountId)
        {
            AccountStat result;
            try
            {
                result = await service.GetCachedAccountStatistics(accountId);
            }
            catch (ArgumentNullException)
            {
                result = null;
            }

            return result;
        }
    }
}