﻿using Autofac;
using Common.Log;
using Lykke.Common.Chaos;
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
            builder.RegisterInstance(_settings.Nested(s => s.MarginTradingAccountManagement)).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.MarginTradingAccountManagement).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.MarginTradingAccountManagement.Cqrs.ContextNames).SingleInstance();
            builder.RegisterType<SystemClock>().As<ISystemClock>().SingleInstance();
            builder.RegisterInstance(_log).As<ILog>().SingleInstance();
            
            builder.RegisterType<EventSender>().As<IEventSender>().SingleInstance();
            builder.RegisterChaosKitty(_settings.CurrentValue.MarginTradingAccountManagement.ChaosKitty);

            RegisterServices(builder);
            RegisterRepositories(builder);
        }

        private void RegisterRepositories(ContainerBuilder builder)
        {
            if (_settings.CurrentValue.MarginTradingAccountManagement.Db.StorageMode == StorageMode.SqlServer.ToString())
            {
                builder.RegisterType<SqlRepos.LogRepository>().As<ILogRepository>().SingleInstance();
                

                builder.RegisterType<SqlRepos.AccountBalanceChangesRepository>()
                    .As<IAccountBalanceChangesRepository>().SingleInstance();
                builder.RegisterType<SqlRepos.AccountsRepository>()
                    .As<IAccountsRepository>().SingleInstance();
                builder.RegisterType<SqlRepos.OperationExecutionInfoRepository>()
                    .As<IOperationExecutionInfoRepository>().SingleInstance();
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
            builder.RegisterType<SendBalanceCommandsService>().As<ISendBalanceCommandsService>().SingleInstance();
            builder.RegisterType<TradingConditionsService>().As<ITradingConditionsService>().SingleInstance();
            
            builder.RegisterType<ConvertService>().As<IConvertService>().SingleInstance();
            builder.RegisterType<RabbitMqService>().As<IRabbitMqService>().SingleInstance(); 
        }
    }
}