// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;

using Common.Log;

using Lykke.Common.Chaos;
using Lykke.SettingsReader;
using Lykke.Snow.Common.Startup;

using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.Infrastructure.Implementation;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Services.Implementation;
using MarginTrading.AccountsManagement.Settings;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

using Module = Autofac.Module;

namespace MarginTrading.AccountsManagement.Modules
{
    internal class AccountsManagementModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;

        public AccountsManagementModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            // still required for some middlewares
            builder.Register(ctx =>
                    new LykkeLoggerAdapter<AccountsManagementModule>(ctx.Resolve<ILogger<AccountsManagementModule>>()))
                .As<ILog>()
                .SingleInstance();
            
            builder.RegisterInstance(_settings.Nested(s => s.MarginTradingAccountManagement)).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.MarginTradingAccountManagement).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.MarginTradingAccountManagement.Cqrs.ContextNames).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.MarginTradingAccountManagement.Cache).SingleInstance();
            builder.RegisterType<SystemClock>().As<ISystemClock>().SingleInstance();
            
            builder.RegisterType<EventSender>()
                .As<IEventSender>()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();
            
            builder.RegisterChaosKitty(_settings.CurrentValue.MarginTradingAccountManagement.ChaosKitty);

            builder.RegisterType<AccountManagementService>().As<IAccountManagementService>().SingleInstance();
            builder.RegisterType<SendBalanceCommandsService>().As<ISendBalanceCommandsService>()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies).SingleInstance();
            builder.RegisterType<TradingConditionsService>().As<ITradingConditionsService>().SingleInstance();
            builder.RegisterType<NegativeProtectionService>().As<INegativeProtectionService>().SingleInstance();
            builder.RegisterType<AccuracyService>().As<IAccuracyService>().SingleInstance();
            builder.RegisterType<ConvertService>().As<IConvertService>().SingleInstance();
            builder.RegisterType<AuditService>().As<IAuditService>().SingleInstance();
            builder.RegisterType<StartupManager>().As<IStartupManager>().SingleInstance();
            builder.RegisterType<BrokerSettingsCache>().As<IBrokerSettingsCache>()
                .WithParameter(TypedParameter.From(_settings.CurrentValue.MarginTradingAccountManagement.BrokerId))
                .SingleInstance();
            builder.RegisterType<MeteorSender>().As<IMeteorSender>().SingleInstance();
        }
    }
}