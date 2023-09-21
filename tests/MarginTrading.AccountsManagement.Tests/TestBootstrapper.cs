// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
using System.Threading.Tasks;

using Lykke.Cqrs;
using Lykke.Snow.Mdm.Contracts.Api;

using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Tests.Fakes;
using MarginTrading.AssetService.Contracts;
using MarginTrading.Backend.Contracts;
using MarginTrading.TradingHistory.Client;

using Meteor.Client;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Moq;

namespace MarginTrading.AccountsManagement.Tests
{
    internal static class TestBootstrapper
    {
        public static Task<HttpClient> CreateTestClient()
        {
            var application = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder
                        .UseSetting("SettingsUrl", "appsettings.test.json")
                        .UseEnvironment("test")
                        .ConfigureServices(MockServices);
                });

            var client = application.CreateDefaultClient();

            return Task.FromResult(client);
        }

        private static void MockServices(IServiceCollection services)
        {
            services.AddSingleton(new Mock<ICqrsSender>().Object);
            services.AddSingleton(new Mock<ICqrsEngine>().Object);

            // mock external services
            services.AddSingleton(new Mock<IAssetsApi>().Object);
            services.AddSingleton(new Mock<ITradingConditionsApi>().Object);
            services.AddSingleton(new Mock<ITradingInstrumentsApi>().Object);
            services.AddSingleton(new Mock<IScheduleSettingsApi>().Object);
            services.AddSingleton(new Mock<IOrdersApi>().Object);
            services.AddSingleton<IPositionsApi>(new FakePositionsApi());
            services.AddSingleton<IAccountsApi>(new FakeAccountsApi());
            services.AddSingleton(new Mock<IDealsApi>().Object);
                            
            // replace with fake broker settings since the feature management is requires it
            services.AddSingleton<IBrokerSettingsApi>(new FakeBrokerSettingsApi());
            
            services.AddSingleton<IMeteorClient>(new FakeMeteorClient());

            // replace with fake implementation top always get a value from the cache
            services.RemoveAll<IAccountsCache>();
            services.AddSingleton<IAccountsCache, FakeAccountsCache>();
        }
    }
}