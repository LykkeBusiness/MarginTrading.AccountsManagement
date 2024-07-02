// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Services.Implementation;

namespace MarginTrading.AccountsManagement.IntegrationTests.Fakes
{
    internal sealed class FakeAccountsCache : IAccountsCache
    {
        public Task<T> Get<T>(string accountId, AccountsCacheCategory accountsCacheCategory, Func<Task<T>> getValue)
        {
            return Task.FromResult(default(T));
        }

        public Task<T> Get<T>(string accountId, AccountsCacheCategory accountsCacheCategory, Func<Task<(T value, bool shouldCache)>> getValue)
        {
            return Task.FromResult(default(T));
        }

        public async Task Invalidate(string accountId)
        {
            await Task.Delay(100);
        }
    }
}