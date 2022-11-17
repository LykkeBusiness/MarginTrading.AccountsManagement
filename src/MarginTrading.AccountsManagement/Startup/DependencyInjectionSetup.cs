// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;

using Lykke.Common.ApiLibrary.Swagger;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.Snow.Common.Correlation;
using Lykke.Snow.Common.Correlation.Cqrs;
using Lykke.Snow.Common.Correlation.Http;
using Lykke.Snow.Common.Correlation.RabbitMq;
using Lykke.Snow.Common.Startup;
using Lykke.Snow.Common.Startup.ApiKey;
using Lykke.Snow.Mdm.Contracts.BrokerFeatures;

using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Infrastructure.Implementation;
using MarginTrading.AccountsManagement.RabbitMq.Publishers;
using MarginTrading.AccountsManagement.Services.Implementation;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AccountsManagement.Workflow.BrokerSettings;
using MarginTrading.AccountsManagement.Workflow.ProductComplexity;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddEodProcessFinishedSubscriber(settings);
            

            services.AddSingleton<LossPercentagePublisher>();
            services.AddSingleton<IRabbitPublisher<AutoComputedLossPercentageUpdateEvent>>(x => x.GetRequiredService<LossPercentagePublisher>());
            services.AddSingleton<IStartable>(x => x.GetRequiredService<LossPercentagePublisher>());
            services.AddSingleton<IStartStop>(x => x.GetRequiredService<LossPercentagePublisher>());

            return services;
        }
    }
}