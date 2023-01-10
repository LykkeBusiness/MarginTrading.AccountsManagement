using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MarginTrading.AccountsManagement.Settings;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Newtonsoft.Json;

namespace MarginTrading.AccountsManagement.Services.Implementation
{
    public class AccountsCache : IAccountsCache
    {
        private readonly IDistributedCache _cache;
        private readonly ISystemClock _systemClock;
        private readonly CacheSettings _cacheSettings;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly ILogger _logger;

        public AccountsCache(IDistributedCache cache, ISystemClock systemClock, CacheSettings cacheSettings, ILogger<AccountsCache> logger)
        {
            _cache = cache;
            _systemClock = systemClock;
            _cacheSettings = cacheSettings;
            _logger = logger;
            _serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        public Task<T> Get<T>(string accountId, AccountsCacheCategory accountsCacheCategory, Func<Task<T>> getValue)
        {
            return Get(accountId, accountsCacheCategory, async () => (value: await getValue(), shouldCache: true));
        }

        public async Task<T> Get<T>(string accountId, AccountsCacheCategory accountsCacheCategory, Func<Task<(T value, bool shouldCache)>> getValue)
        {
            var cacheKey = BuildCacheKey(accountId, accountsCacheCategory);
            var cached = await _cache.GetStringAsync(BuildCacheKey(accountId, accountsCacheCategory));

            if (cached != null)
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(cached, _serializerSettings);
                }
                catch (JsonSerializationException e) 
                {
                    //serialization settings includes Type Namespace etc.
                    //Mismatch in that parameters (for instance during refactoring of code base) could lead to exception during deserialization.
                    //We should invalidate cache in that case

                    _logger.LogWarning(e,
                        "Type mismatch while deserialization cache item of category {Category} for {AccountId}. Invalidating cache",
                        accountsCacheCategory, accountId);
                }
            }

            var result = await getValue();
            if (result.shouldCache)
            {
                var serialized = JsonConvert.SerializeObject(result.value, _serializerSettings);
                await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheSettings.ExpirationPeriod
                });
            }

            return result.value;
        }

        public async Task Invalidate(string accountId)
        {
            foreach (var cat in Enum.GetValues(typeof(AccountsCacheCategory)).Cast<AccountsCacheCategory>())
            {
                await InvalidateCache(accountId, cat);
            }
        }

        private Task InvalidateCache(string accountId, AccountsCacheCategory accountsCacheCategory)
        {
            var cacheKey = BuildCacheKey(accountId, accountsCacheCategory);
            return _cache.RemoveAsync(cacheKey);
        }

        private string BuildCacheKey(string accountId, AccountsCacheCategory accountsCacheCategory)
        {
            var now = _systemClock.UtcNow.Date;
            return $"ac:{accountId}:{accountsCacheCategory:G}:{now:yyyy-MM-dd}";
        }
    }
}
