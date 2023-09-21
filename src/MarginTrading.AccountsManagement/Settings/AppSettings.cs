// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.AccountsManagement.Settings
{
    [UsedImplicitly]
    internal class AppSettings
    {
        public AccountManagementSettings MarginTradingAccountManagement { get; set; }


        [Optional]
        public Lykke.Snow.Common.Startup.ApiKey.ClientSettings MarginTradingAccountManagementServiceClient { get; set; }
         = new Lykke.Snow.Common.Startup.ApiKey.ClientSettings();
        
        public ClientSettings MarginTradingSettingsServiceClient { get; set; }

        public ClientSettings MdmServiceClient { get; set; }
        
        public OptionalClientSettings MtBackendServiceClient { get; set; }
        
        public OptionalClientSettings TradingHistoryClient { get; set; }
        
        public OptionalClientSettings MeteorServiceClient { get; set; }
    }
}
