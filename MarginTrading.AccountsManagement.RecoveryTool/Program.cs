using System.IO;
using System.Threading.Tasks;

using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.Infrastructure.Implementation;
using MarginTrading.AccountsManagement.RecoveryTool.LogParsers;
using MarginTrading.AccountsManagement.RecoveryTool.Mappers;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Repositories.Implementation.SQL;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.RecoveryTool
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            var app = host.Services.GetService<App>();
            await app.ImportFromAccountManagementAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.SetBasePath(Directory.GetCurrentDirectory());
                    builder.AddJsonFile("appsettings.json");
                })
                .ConfigureLogging(x => x.AddConsole())
                .ConfigureServices((context, services) =>
                {
                    // db connection
                    var cs = context.Configuration.GetConnectionString("db");

                    // container
                    services.AddSingleton(context.Configuration);
                    services.AddSingleton<App>();
                    services.AddSingleton<AccountsManagementLogParser>();

                    services.AddSingleton<UpdateBalanceInternalCommandMapper>();
                    services.AddSingleton<AccountChangedEventMapper>();

                    services.AddSingleton<ISystemClock, SystemClock>();
                    services.AddSingleton<IConvertService, ConvertService>();
                    services.AddSingleton<IAccountsRepository, AccountsRepository>(provider =>
                        ActivatorUtilities.CreateInstance<AccountsRepository>(provider, cs, 60));

                });

            return hostBuilder;
        }
    }
}