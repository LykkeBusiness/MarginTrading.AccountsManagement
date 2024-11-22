// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using Lykke.Logs.Serilog;
using Lykke.SettingsReader;
using Lykke.SettingsReader.ConfigurationProvider;

using MarginTrading.AccountsManagement.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace MarginTrading.AccountsManagement.Startup
{
    internal static class ConfigurationBuilder
    {
        public static (IConfigurationRoot, IReloadingManager<AppSettings>) BuildConfiguration(
            this WebApplicationBuilder builder)
        {
            builder.Environment.ContentRootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var configurationBuilder = builder.Configuration
                .SetBasePath(builder.Environment.ContentRootPath)
                .AddSerilogJson(builder.Environment)
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables();

            if (Environment.GetEnvironmentVariable("SettingsUrl")?.StartsWith("http") ?? false)
            {
                configurationBuilder.AddHttpSourceConfiguration();
            }

            var configuration = configurationBuilder.Build();

            var settingsManager = configuration.LoadSettings<AppSettings>(_ => { });

            return (configuration, settingsManager);
        }
    }
}