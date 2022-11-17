// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Logs.Serilog;
using MarginTrading.AccountsManagement.Startup;
using Microsoft.AspNetCore.Builder;

namespace MarginTrading.AccountsManagement
{
    [UsedImplicitly]
    internal sealed class Program
    {
        public static async Task Main(string[] args)
        {
            await StartupLoggingWrapper.HandleStartupException(async () =>
            {
                var builder = WebApplication.CreateBuilder(args);
            
                var (configuration, settingsManager) = builder.BuildConfiguration();
            
                builder.Services.RegisterInfrastructureServices(settingsManager.CurrentValue);
            
                builder.ConfigureHost(configuration, settingsManager);
            
                var app = builder.Build();
            
                await app
                    .Configure()
                    .RunAsync();
            }, "accounts");
        }
    }
}
