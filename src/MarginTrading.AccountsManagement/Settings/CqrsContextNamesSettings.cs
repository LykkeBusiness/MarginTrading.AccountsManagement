// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.SettingsReader.Attributes;

namespace MarginTrading.AccountsManagement.Settings
{
    public class CqrsContextNamesSettings
    {
        [Optional] public string AccountsManagement { get; set; } = nameof(AccountsManagement);

        [Optional] public string TradingEngine { get; set; } = nameof(TradingEngine);
        
        [Optional] public string BookKeeper { get; set; } = nameof(BookKeeper);
        
        [Optional] public string Mdm { get; set; } = nameof(Mdm);
    }
}