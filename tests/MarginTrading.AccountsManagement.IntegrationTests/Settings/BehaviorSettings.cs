using JetBrains.Annotations;

using Lykke.SettingsReader.Attributes;

namespace MarginTrading.AccountsManagement.IntegrationTests.Settings
{
    [UsedImplicitly]
    public class BehaviorSettings
    {
        [Optional]
        public string AccountIdPrefix { get; set; }
        
        [Optional]
        public decimal DefaultBalance { get; set; }
        
        [Optional]
        public bool BalanceResetIsEnabled { get; set; }
    }
}