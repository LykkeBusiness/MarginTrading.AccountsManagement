using JetBrains.Annotations;

namespace MarginTrading.AccountsManagement.IntegrationTests.Settings
{
    [UsedImplicitly]
    public class RabbitMqSettings
    {
        public RabbitConnectionSettings AccountChangedExchange { get; set; }
        public RabbitConnectionSettings AccountHistoryExchange { get; set; }
    }
}