// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Net.Http;

using IdentityModel.Client;

using Lykke.Snow.Common.Startup;
using Lykke.Snow.Common.Startup.Authorization;

using MarginTrading.AccountsManagement.Settings;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Extensions
{
    public static class SecurityConfigurationExtensions
    {
        /// <summary>
        /// Adds identity model TokenClient and <see cref="AccessTokenDelegatingHandler"/> into DI container
        /// </summary>
        /// <param name="services"></param>
        /// <param name="authority"></param>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="clientScope"></param>
        /// <param name="renewTokenTimeoutSec"></param>
        /// <param name="validateIssuerName"></param>
        /// <param name="requireHttps"></param>
        public static void AddDelegatingHandler(
            this IServiceCollection services,
            string authority,
            string clientId,
            string clientSecret,
            string clientScope,
            int? renewTokenTimeoutSec,
            bool validateIssuerName,
            bool requireHttps)
        {
            services.AddTokenClient(authority, clientId, clientSecret, validateIssuerName, requireHttps);

            services.AddSingleton(provider => new AccessTokenDelegatingHandler(
                provider.GetService<TokenClient>(),
                clientScope,
                new HttpClientHandler(),
                provider.GetService<ILogger<AccessTokenDelegatingHandler>>(),
                renewTokenTimeoutSec));

            services.AddSingleton<HttpMessageHandler>(provider => provider.GetService<AccessTokenDelegatingHandler>());
        }

        public static void AddDelegatingHandler(this IServiceCollection services, OidcSettings configuration)
        {
            services.AddDelegatingHandler(configuration.ApiAuthority,
                configuration.ClientId,
                configuration.ClientSecret,
                configuration.ClientScope,
                configuration.RenewTokenTimeoutSec,
                configuration.ValidateIssuerName,
                configuration.RequireHttps);
        }
    }
}