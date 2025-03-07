﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Autofac;
using Lykke.HttpClientGenerator;
using Lykke.MarginTrading.BrokerBase;
using Lykke.MarginTrading.BrokerBase.Models;
using Lykke.MarginTrading.BrokerBase.Settings;
using Lykke.SettingsReader;
using Lykke.SettingsReader.SettingsTemplate;
using MarginTrading.AccountsManagement.AccountHistoryBroker.Models;
using MarginTrading.AccountsManagement.AccountHistoryBroker.Repositories;
using MarginTrading.AccountsManagement.AccountHistoryBroker.Services;
using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.AccountsManagement.Contracts.Events;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlRepos = MarginTrading.AccountsManagement.AccountHistoryBroker.Repositories.SqlRepositories;
using Microsoft.Extensions.Hosting;

namespace MarginTrading.AccountsManagement.AccountHistoryBroker
{
    public class Startup : BrokerStartupBase<DefaultBrokerApplicationSettings<Settings>, Settings>
    {
        protected override string ApplicationName => "AccountHistoryBroker";

        public Startup(IHostEnvironment env, IConfiguration configuration) : base(env, configuration)
        {
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddSettingsTemplateGenerator();
        }

        protected override void ConfigureEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapSettingsTemplate();
        }

        protected override void RegisterCustomServices(ContainerBuilder builder,
            IReloadingManager<Settings> settings)
        {
            builder.AddMessagePackBrokerMessagingFactory<AccountChangedEvent>();
            builder.RegisterClient<IAccountsApi>(settings.CurrentValue.AccountManagement.ServiceUrl,
                config =>
                    config
                        .WithServiceName<LykkeErrorResponse>($"Account Management")
                        .WithOptionalApiKey(settings.CurrentValue.AccountManagement.ApiKey));

            builder.RegisterType<Application>().As<IBrokerApplication>().SingleInstance();
            builder.RegisterInstance(new ConvertService())
                .As<IConvertService>().SingleInstance();

            if (settings.CurrentValue.Db.StorageMode == StorageMode.SqlServer.ToString())
            {
                builder.RegisterType<SqlRepos.AccountHistoryRepository>()
                    .As<IAccountHistoryRepository>()
                    .SingleInstance();
            }
            else if (settings.CurrentValue.Db.StorageMode == StorageMode.Azure.ToString())
            {
                throw new InvalidOperationException("Azure storage mode is not supported");
            }

            builder.RegisterType<TaxHistoryInsertedPublisher>();
        }
    }
}