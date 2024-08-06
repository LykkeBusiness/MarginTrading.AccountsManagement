// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lykke.SettingsReader;
using Lykke.Snow.Common.Correlation;
using Lykke.Snow.Common.Correlation.Serilog;

using MarginTrading.AccountsManagement.Extensions;
using MarginTrading.AccountsManagement.Modules;
using MarginTrading.AccountsManagement.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MarginTrading.AccountsManagement.Startup
{
    internal static class HostConfiguration
    {
        public static IHostBuilder ConfigureHost(
            this WebApplicationBuilder builder,
            IConfiguration configuration,
            IReloadingManager<AppSettings> settings) =>
            builder.Host
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>((ctx, cBuilder) =>
                {
                    cBuilder.RegisterModule(new AccountsManagementModule(settings));
                    if (!ctx.HostingEnvironment.IsTest())
                    {
                        cBuilder.RegisterModule(
                            new RabbitMqModule(settings.CurrentValue.MarginTradingAccountManagement.RabbitMq));
                        cBuilder.RegisterModule(new DataModule(settings));
                        cBuilder.RegisterModule(new AccountsManagementExternalServicesModule(settings));
                        cBuilder.RegisterModule(
                            new CqrsModule(settings.CurrentValue.MarginTradingAccountManagement.Cqrs));
                    }
                })
                .UseSerilog((_, cfg) =>
                {
                    var a = typeof(Program).Assembly;
                    var title =
                        a.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ??
                        string.Empty;
                    var version =
                        a.GetCustomAttribute<
                                AssemblyInformationalVersionAttribute>()
                            ?.InformationalVersion ?? string.Empty;
                    var environment =
                        Environment.GetEnvironmentVariable(
                            "ASPNETCORE_ENVIRONMENT") ?? string.Empty;

                    cfg.ReadFrom.Configuration(configuration)
                        .Enrich.WithProperty("Application", title)
                        .Enrich.WithProperty("Version", version)
                        .Enrich.WithProperty("Environment", environment)
                        .Enrich.WithProperty("BrokerId",
                            settings.CurrentValue.MarginTradingAccountManagement.BrokerId)
                        .Enrich.With(new CorrelationLogEventEnricher(
                            "CorrelationId", new CorrelationContextAccessor()));
                });
    }
}