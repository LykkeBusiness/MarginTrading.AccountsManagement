// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.AccountsManagement.Settings
{
    public class CacheSettings
    {
        public TimeSpan ExpirationPeriod { get; set; }
        public string RedisConfiguration { get; set; }
    }
}