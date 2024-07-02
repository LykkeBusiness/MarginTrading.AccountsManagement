using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Lykke.SettingsReader;

using MarginTrading.AccountsManagement.IntegrationTests.Settings;

using Microsoft.Extensions.Configuration;

namespace MarginTrading.AccountsManagement.IntegrationTests.Infrastructure
{
    internal class SettingsUtil
    {
        public static AppSettings Settings { get; } = GetSettings();

        private static AppSettings GetSettings()
        {
            var baseDir = GetExecutingAssemblyDir();
            var builder = new ConfigurationBuilder()
                .SetBasePath(baseDir)
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"SettingsUrl", Path.Combine(baseDir, "appsettings.dev.json")}
                })
                .AddEnvironmentVariables();
            return builder.Build().LoadSettings<AppSettings>().CurrentValue;
        }

        private static string GetExecutingAssemblyDir()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }
}