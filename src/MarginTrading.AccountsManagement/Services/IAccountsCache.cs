// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using MarginTrading.AccountsManagement.Services.Implementation;

namespace MarginTrading.AccountsManagement.Services
{
    public interface IAccountsCache
    {
        Task<T> Get<T>(string accountId, AccountsCacheCategory accountsCacheCategory, Func<Task<T>> getValue);

        Task<T> Get<T>(string accountId,
            AccountsCacheCategory accountsCacheCategory,
            Func<Task<(T value, bool shouldCache)>> getValue);

        Task Invalidate(string accountId);
    }
}