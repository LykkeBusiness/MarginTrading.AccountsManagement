// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lykke.HttpClientGenerator;
using Lykke.SettingsReader;
using Lykke.Snow.Common.Startup.Authorization;
using Lykke.Snow.Common.Startup.HttpClientGenerator;
using Lykke.Snow.Mdm.Contracts.Api;
using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.Backend.Contracts;
using MarginTrading.AssetService.Contracts;
using MarginTrading.TradingHistory.Client;

using Meteor.Client;

using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.AccountsManagement.Modules
{
    internal class AccountsManagementExternalServicesModule : Module
    {
        private readonly IServiceCollection _services = new ServiceCollection();
        private readonly IReloadingManager<AppSettings> _settings;

        public AccountsManagementExternalServicesModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var settingsClientGeneratorBuilder = HttpClientGenerator
                .BuildForUrl(_settings.CurrentValue.MarginTradingSettingsServiceClient.ServiceUrl)
                .WithServiceName<LykkeErrorResponse>(
                    $"MT Settings [{_settings.CurrentValue.MarginTradingSettingsServiceClient.ServiceUrl}]");
            
            if (!string.IsNullOrWhiteSpace(_settings.CurrentValue.MarginTradingSettingsServiceClient.ApiKey))
            {
                settingsClientGeneratorBuilder = settingsClientGeneratorBuilder
                    .WithApiKey(_settings.CurrentValue.MarginTradingSettingsServiceClient.ApiKey);
            }

            var settingsClientGenerator = settingsClientGeneratorBuilder.Create();

            builder.RegisterInstance(settingsClientGenerator.Generate<IAssetsApi>());
            builder.RegisterInstance(settingsClientGenerator.Generate<ITradingConditionsApi>());
            builder.RegisterInstance(settingsClientGenerator.Generate<ITradingInstrumentsApi>());
            builder.RegisterInstance(settingsClientGenerator.Generate<IScheduleSettingsApi>());

            var mtCoreClientGenerator = HttpClientGenerator
                .BuildForUrl(_settings.CurrentValue.MtBackendServiceClient.ServiceUrl)
                .WithApiKey(_settings.CurrentValue.MtBackendServiceClient.ApiKey)
                .WithServiceName<LykkeErrorResponse>(
                    $"MT Trading Core [{_settings.CurrentValue.MtBackendServiceClient.ServiceUrl}]")
                .Create();

            builder.RegisterInstance(mtCoreClientGenerator.Generate<IOrdersApi>());
            builder.RegisterInstance(mtCoreClientGenerator.Generate<IPositionsApi>());
            builder.RegisterInstance(mtCoreClientGenerator.Generate<IAccountsApi>());
            
            var mtTradingHistoryClientGenerator = HttpClientGenerator
                .BuildForUrl(_settings.CurrentValue.TradingHistoryClient.ServiceUrl)
                .WithServiceName<LykkeErrorResponse>("MT Core History Service")
                .WithOptionalApiKey(_settings.CurrentValue.TradingHistoryClient.ApiKey)
                .Create();

            builder.RegisterInstance(mtTradingHistoryClientGenerator.Generate<IDealsApi>());

            var mdmClientGenerator = HttpClientGenerator
                .BuildForUrl(_settings.CurrentValue.MdmServiceClient.ServiceUrl)
                .WithServiceName<LykkeErrorResponse>("Mdm Service")
                .WithOptionalApiKey(_settings.CurrentValue.MdmServiceClient.ApiKey)
                .Create();

            builder.RegisterInstance(mdmClientGenerator.Generate<IBrokerSettingsApi>());
            
            builder.Register(ctx =>
                {
                    var meteorClientGenerator = HttpClientGenerator
                        .BuildForUrl(_settings.CurrentValue.MeteorServiceClient.ServiceUrl)
                        .WithServiceName<LykkeErrorResponse>("Meteor Service")
                        .WithAdditionalDelegatingHandler(ctx.Resolve<AccessTokenDelegatingHandler>())
                        .WithAdditionalDelegatingHandler(
                            ctx.Resolve<NotSuccessStatusCodeDelegatingHandler>())
                        .Create();

                    return meteorClientGenerator.Generate<IMeteorClient>();
                })
                .As<IMeteorClient>()
                .SingleInstance();

            builder.Populate(_services);
        }
    }
}