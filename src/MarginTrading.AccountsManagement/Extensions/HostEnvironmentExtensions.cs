// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;

namespace MarginTrading.AccountsManagement.Extensions
{
    public static class HostEnvironmentExtensions
    {
        public static bool IsTest(this IHostEnvironment environment)
        {
            return environment.IsEnvironment("test");
        }
    }
}