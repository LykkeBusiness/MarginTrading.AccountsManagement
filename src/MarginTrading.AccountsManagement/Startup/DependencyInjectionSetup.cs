// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.IO;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Snow.Common.Correlation;
using Lykke.Snow.Common.Correlation.Cqrs;
using Lykke.Snow.Common.Correlation.Http;
using Lykke.Snow.Common.Correlation.RabbitMq;
using Lykke.Snow.Common.Startup;
using Lykke.Snow.Common.Startup.ApiKey;
using Lykke.Snow.Mdm.Contracts.BrokerFeatures;
using MarginTrading.AccountsManagement.Infrastructure.Implementation;
using MarginTrading.AccountsManagement.Services.Implementation;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AccountsManagement.Workflow.BrokerSettings;
using MarginTrading.AccountsManagement.Workflow.ProductComplexity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace MarginTrading.AccountsManagement.Startup
{
    internal static class DependencyInjectionSetup
    {
        private static readonly string ApiName = "Nova 2 Accounts Management API";

        public static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services, 
            AppSettings settings)
        {
            services
                .AddApplicationInsightsTelemetry()
                .AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                });

            services.AddApiKeyAuth(settings.MarginTradingAccountManagementServiceClient);

            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration("v1", ApiName);
                var contractsXmlPath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, 
                    "MarginTrading.AccountsManagement.Contracts.xml");
                options.IncludeXmlComments(contractsXmlPath);
                options.OperationFilter<CustomOperationIdOperationFilter>();
                if (!string.IsNullOrWhiteSpace(settings.MarginTradingAccountManagementServiceClient?.ApiKey))
                {
                    options.AddApiKeyAwareness();
                }
            }).AddSwaggerGenNewtonsoftSupport();

            services.AddStackExchangeRedisCache(o =>
            {
                o.Configuration = settings.MarginTradingAccountManagement.Cache.RedisConfiguration;
                o.InstanceName = "AccountManagement:";
            });

            services.AddSingleton<AccountsCache>();

            var correlationContextAccessor = new CorrelationContextAccessor();
            services.AddSingleton(correlationContextAccessor);
            services.AddSingleton<RabbitMqCorrelationManager>();
            services.AddSingleton<CqrsCorrelationManager>();
            services.AddTransient<HttpCorrelationHandler>();

            services.AddFeatureManagement(settings.MarginTradingAccountManagement.BrokerId);
            services.AddProductComplexity(settings);
            services.AddBrokerSettings(settings);

            return services;
        }
    }
}