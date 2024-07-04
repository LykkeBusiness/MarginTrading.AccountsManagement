// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using Autofac;

using Lykke.Logs.MsSql.Interfaces;
using Lykke.Logs.MsSql.Repositories;
using Lykke.SettingsReader;

using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Settings;

namespace MarginTrading.AccountsManagement.Modules
{
    internal class DataModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;

        public DataModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            if (_settings.CurrentValue.MarginTradingAccountManagement.Db.StorageMode == StorageMode.Azure.ToString())
            {
                throw new InvalidOperationException("Azure mode is not supported");
            }

            builder.RegisterType<SqlLogRepository>().As<ILogRepository>().SingleInstance();

            builder.RegisterType<Repositories.Implementation.SQL.AccountBalanceChangesRepository>()
                .As<IAccountBalanceChangesRepository>()
                .SingleInstance();

            builder.RegisterType<Repositories.Implementation.SQL.LossPercentageRepository>()
                .As<ILossPercentageRepository>().SingleInstance();
            builder.RegisterType<Repositories.Implementation.SQL.AccountHistoryRepository>()
                .As<IAccountHistoryRepository>().SingleInstance();
            builder.RegisterType<Repositories.Implementation.SQL.OperationExecutionInfoRepository>()
                .As<IOperationExecutionInfoRepository>().SingleInstance();
            builder.RegisterType<Repositories.Implementation.SQL.SqlEodTaxFileMissingRepository>()
                .As<IEodTaxFileMissingRepository>()
                .WithParameter(
                    TypedParameter.From(_settings.CurrentValue.MarginTradingAccountManagement.Db.ConnectionString))
                .SingleInstance();

            builder.RegisterType<Repositories.Implementation.SQL.ComplexityWarningRepository>()
                .As<IComplexityWarningRepository>()
                .WithParameter(
                    TypedParameter.From(_settings.CurrentValue.MarginTradingAccountManagement.Db.ConnectionString))
                .SingleInstance();

            builder.RegisterType<Repositories.Implementation.SQL.AuditRepository>()
                .As<IAuditRepository>()
                .WithParameter(
                    TypedParameter.From(_settings.CurrentValue.MarginTradingAccountManagement.Db.ConnectionString))
                .SingleInstance();
        }
    }
}