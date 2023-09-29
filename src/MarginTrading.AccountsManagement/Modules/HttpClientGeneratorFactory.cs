// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using Lykke.HttpClientGenerator;

using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.Settings;

namespace MarginTrading.AccountsManagement.Modules
{
    internal static class HttpClientGeneratorFactory
    {
        static HttpClientGeneratorBuilder CreateBuilder(string serviceName, 
            OptionalClientSettings optionalClientSettings)
        {
            return CreateBuilder(serviceName,
                new ClientSettings
                {
                    ApiKey = optionalClientSettings.ApiKey, ServiceUrl = optionalClientSettings.ServiceUrl
                });
        }
        
        static HttpClientGeneratorBuilder CreateBuilder(string serviceName, ClientSettings clientSettings)
        {
            if (clientSettings == null)
                throw new ArgumentNullException(nameof(clientSettings));
            
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentNullException(nameof(serviceName));

            var builder = HttpClientGenerator
                .BuildForUrl(clientSettings.ServiceUrl)
                .WithOptionalApiKey(clientSettings.ApiKey)
                .WithServiceName<LykkeErrorResponse>(
                    $"{serviceName} [{clientSettings.ServiceUrl}]");
            
            return builder;
        }

        /// <summary>
        /// Create HttpClientGenerator for specified service name and client settings
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="clientSettings"></param>
        /// <returns></returns>
        public static HttpClientGenerator Create(string serviceName, ClientSettings clientSettings) =>
            CreateBuilder(serviceName, clientSettings).Create();

        /// <summary>
        /// Create HttpClientGenerator for specified service name and optional client settings
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="optionalClientSettings"></param>
        /// <returns></returns>
        public static HttpClientGenerator Create(string serviceName, OptionalClientSettings optionalClientSettings) =>
            CreateBuilder(serviceName, optionalClientSettings).Create();
    }
}