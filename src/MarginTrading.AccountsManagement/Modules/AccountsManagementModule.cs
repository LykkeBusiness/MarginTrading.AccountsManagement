// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;
using Common.Log;
using Lykke.Common.Chaos;
using Lykke.Logs.MsSql.Interfaces;
using Lykke.Logs.MsSql.Repositories;
using Lykke.SettingsReader;
using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.Infrastructure.Implementation;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Repositories.AzureServices;
using MarginTrading.AccountsManagement.Repositories.AzureServices.Implementations;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Services.Implementation;
using MarginTrading.AccountsManagement.Settings;
using Microsoft.Extensions.Internal;
using Module = Autofac.Module;
using AzureRepos = MarginTrading.AccountsManagement.Repositories.Implementation.AzureStorage;
using SqlRepos = MarginTrading.AccountsManagement.Repositories.Implementation.SQL;

namespace MarginTrading.AccountsManagement.Modules
{
    internal class AccountsManagementModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;
        private readonly ILog _log;

        public AccountsManagementModule(IReloadingManager<AppSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(new BrokerConfigurationAccessor(_settings.CurrentValue.MarginTradingAccountManagement.BrokerId));
            builder.RegisterInstance(_settings.Nested(s => s.MarginTradingAccountManagement)).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.MarginTradingAccountManagement).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.MarginTradingAccountManagement.Cqrs.ContextNames).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.MarginTradingAccountManagement.Cache).SingleInstance();
            builder.RegisterType<SystemClock>().As<ISystemClock>().SingleInstance();
            builder.RegisterInstance(_log).As<ILog>().SingleInstance();
            
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
                    .As<IAccountBalanceChangesRepository>().SingleInstance();
                builder.RegisterType<SqlRepos.AccountsRepository>()
                    .As<IAccountsRepository>()
                    .WithParameter(TypedParameter.From(_settings.CurrentValue.MarginTradingAccountManagement.Db.ConnectionString))
                    .WithParameter(TypedParameter.From(_settings.CurrentValue.MarginTradingAccountManagement.Db.LongRunningSqlTimeoutSec))
                    .SingleInstance();
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
                builder.RegisterType<AzureTableStorageFactoryService>().As<IAzureTableStorageFactoryService>()
                    .SingleInstance();

                builder.RegisterType<AzureRepos.AccountBalanceChangesRepository>()
                    .As<IAccountBalanceChangesRepository>().SingleInstance();
                builder.RegisterType<AzureRepos.AccountsRepository>()
                    .As<IAccountsRepository>().SingleInstance();
                builder.RegisterType<AzureRepos.OperationExecutionInfoRepository>()
                    .As<IOperationExecutionInfoRepository>().SingleInstance();
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
        }
    }
}