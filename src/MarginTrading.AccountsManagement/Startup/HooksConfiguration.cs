// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.AccountsManagement.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Startup
{
    internal static class HooksConfiguration
    {
        public static void RegisterHooks(this WebApplication app)
        {
            app.Lifetime.ApplicationStarted.Register(() => OnApplicationStarted(app));
            app.Lifetime.ApplicationStopping.Register(() => OnApplicationStopping(app));
        }

        private static void OnApplicationStarted(WebApplication app)
        {
            var log = app.Services.GetService<ILogger<Program>>();
            var startupManager = app.Services.GetRequiredService<IStartupManager>();

            try
            {
                startupManager.Start();
                log?.LogInformation("Nova 2 Accounts Management API started");
            }
            catch (Exception ex)
            {
                log?.LogCritical(ex, "Error on startup");
                startupManager.Stop();
                app.StopAsync().GetAwaiter().GetResult();
            }
        }

        private static void OnApplicationStopping(WebApplication app)
        {
            var log = app.Services.GetService<ILogger<Program>>();
            var startupManager = app.Services.GetRequiredService<IStartupManager>();
            startupManager.Stop();
            log?.LogInformation("Nova 2 Accounts Management API stopping");
        }
    }
}