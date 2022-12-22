// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Autofac;

using Common.Log;

using Lykke.Common.Chaos;
using Lykke.Logs.MsSql.Interfaces;
using Lykke.Logs.MsSql.Repositories;
using Lykke.SettingsReader;
using Lykke.Snow.Common.Startup;

using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.Infrastructure.Implementation;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Services.Implementation;
using MarginTrading.AccountsManagement.Settings;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

using Module = Autofac.Module;
using SqlRepos = MarginTrading.AccountsManagement.Repositories.Implementation.SQL;

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
            
            builder.RegisterInstance(new BrokerConfigurationAccessor(_settings.CurrentValue.MarginTradingAccountManagement.BrokerId));
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

            RegisterServices(builder);
            RegisterRepositories(builder);
        }

        private void RegisterRepositories(ContainerBuilder builder)
        {
            if (_settings.CurrentValue.MarginTradingAccountManagement.Db.StorageMode == StorageMode.SqlServer.ToString())
            {
                builder.RegisterType<SqlLogRepository>().As<ILogRepository>().SingleInstance();
                
                builder.RegisterType<SqlRepos.AccountBalanceChangesRepository>()
                    .As<IAccountBalanceChangesRepository>()
                    .SingleInstance();
                
                builder.RegisterType<SqlRepos.AccountsRepository>()
                    .As<IAccountsRepository>()
                    .WithParameter(TypedParameter.From(_settings.CurrentValue.MarginTradingAccountManagement.Db.ConnectionString))
                    .WithParameter(TypedParameter.From(_settings.CurrentValue.MarginTradingAccountManagement.Db.LongRunningSqlTimeoutSec))
                    .SingleInstance();
                builder.RegisterDecorator<SqlRepos.AccountsRepositoryLoggingDecorator, IAccountsRepository>();
                
                builder.RegisterType<SqlRepos.OperationExecutionInfoRepository>()
                    .As<IOperationExecutionInfoRepository>().SingleInstance();
                builder.RegisterType<SqlRepos.SqlEodTaxFileMissingRepository>()
                    .As<IEodTaxFileMissingRepository>()
                    .WithParameter(TypedParameter.From(_settings.CurrentValue.MarginTradingAccountManagement.Db.ConnectionString))
                    .SingleInstance();

                builder.RegisterType<SqlRepos.ComplexityWarningRepository>()
                    .As<IComplexityWarningRepository>()
                    .WithParameter(TypedParameter.From(_settings.CurrentValue.MarginTradingAccountManagement.Db.ConnectionString))
                    .SingleInstance();

                builder.RegisterType<SqlRepos.AuditRepository>()
                    .As<IAuditRepository>()
                    .WithParameter(TypedParameter.From(_settings.CurrentValue.MarginTradingAccountManagement.Db.ConnectionString))
                    .SingleInstance();
            }
            else if (_settings.CurrentValue.MarginTradingAccountManagement.Db.StorageMode == StorageMode.Azure.ToString())
            {
                throw new InvalidOperationException("Azure mode is not supported");
            }
        }

        private void RegisterServices(ContainerBuilder builder)
        {
            builder.RegisterType<AccountManagementService>().As<IAccountManagementService>().SingleInstance();
            builder.RegisterType<SendBalanceCommandsService>().As<ISendBalanceCommandsService>()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies).SingleInstance();
            builder.RegisterType<TradingConditionsService>().As<ITradingConditionsService>().SingleInstance();
            builder.RegisterType<NegativeProtectionService>().As<INegativeProtectionService>().SingleInstance();
            builder.RegisterType<AccuracyService>().As<IAccuracyService>().SingleInstance();
            builder.RegisterType<ConvertService>().As<IConvertService>().SingleInstance();
            builder.RegisterType<RabbitMqService>().As<IRabbitMqService>().SingleInstance();
            builder.RegisterType<AuditService>().As<IAuditService>().SingleInstance();
            builder.RegisterType<StartupManager>().As<IStartupManager>().SingleInstance();
            builder.RegisterType<BrokerSettingsCache>().As<IBrokerSettingsCache>()
                .WithParameter(TypedParameter.From(_settings.CurrentValue.MarginTradingAccountManagement.BrokerId))
                .SingleInstance();
        }
    }
}