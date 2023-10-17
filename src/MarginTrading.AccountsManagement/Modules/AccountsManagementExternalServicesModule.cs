// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;

using Lykke.HttpClientGenerator;
using Lykke.SettingsReader;
using Lykke.Snow.Mdm.Contracts.Api;
using Lykke.Snow.Common.Startup.Authorization;
using Lykke.Snow.Common.Startup.HttpClientGenerator;

using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.Backend.Contracts;
using MarginTrading.AssetService.Contracts;
using MarginTrading.TradingHistory.Client;

using Meteor.Client;

namespace MarginTrading.AccountsManagement.Modules
{
    internal class AccountsManagementExternalServicesModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;

        public AccountsManagementExternalServicesModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            # region Asset Service Clients
            var settingsClientGenerator = HttpClientGeneratorFactory.Create(
                "MT Settings",
                _settings.CurrentValue.MarginTradingSettingsServiceClient);

            builder.RegisterInstance(settingsClientGenerator.Generate<IAssetsApi>());
            builder.RegisterInstance(settingsClientGenerator.Generate<ITradingConditionsApi>());
            builder.RegisterInstance(settingsClientGenerator.Generate<ITradingInstrumentsApi>());
            builder.RegisterInstance(settingsClientGenerator.Generate<IScheduleSettingsApi>());
            # endregion

            # region Trading Core Clients
            var mtCoreClientGenerator = HttpClientGeneratorFactory.Create(
                "MT Trading Core", 
                _settings.CurrentValue.MtBackendServiceClient);

            builder.RegisterInstance(mtCoreClientGenerator.Generate<IOrdersApi>());
            builder.RegisterInstance(mtCoreClientGenerator.Generate<IPositionsApi>());
            // <see cref="IAccountsApi"/> is decorated with <see cref="AccountsApiHttpContextDecorator"/>
            // and registered in <see cref="DependencyInjectionSetup" with ASP.NET Core DI container/>
            # endregion
            
            # region Trading History Clients
            var mtTradingHistoryClientGenerator = HttpClientGeneratorFactory.Create(
                "MT Core History Service",
                _settings.CurrentValue.TradingHistoryClient);

            builder.RegisterInstance(mtTradingHistoryClientGenerator.Generate<IDealsApi>());
            # endregion

            # region Mdm Service Clients
            var mdmClientGenerator =
                HttpClientGeneratorFactory.Create("Mdm Service", _settings.CurrentValue.MdmServiceClient);

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
            # endregion
        }
    }
}