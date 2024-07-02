using JetBrains.Annotations;

namespace MarginTrading.AccountsManagement.IntegrationTests.Settings
{
    [UsedImplicitly]
    internal class AppSettings
    {
        public AccountManagementSettings MarginTradingAccountManagement { get; set; }
        public AccountManagementServiceClientSettings MarginTradingAccountManagementServiceClient { get; set; }
    }
}
