using Lykke.SettingsReader.Attributes;

namespace MarginTrading.AccountsManagement.IntegrationTests.Settings
{
    public class CqrsContextNamesSettings
    {
        [Optional] public string AccountsManagement { get; set; } = nameof(AccountsManagement);

        [Optional] public string TradingEngine { get; set; } = nameof(TradingEngine);
    }
}