using Lykke.HttpClientGenerator;

using MarginTrading.AccountsManagement.Contracts;

namespace MarginTrading.AccountsManagement.IntegrationTests.Infrastructure
{
    public static class ClientUtil
    {
        public static IAccountsApi AccountsApi { get; } = GetClient();

        private static IAccountsApi GetClient()
        {
            var generator = HttpClientGenerator.BuildForUrl(
                    SettingsUtil.Settings.MarginTradingAccountManagementServiceClient.ServiceUrl)
                .WithoutCaching().WithoutRetries().Create();
            return generator.Generate<IAccountsApi>();
        }
    }
}