﻿using Lykke.SettingsReader.Attributes;

namespace MarginTrading.AccountsManagement.BrokerBase.Settings
{
    public class BrokerSettingsBase
    {
        public string MtRabbitMqConnString { get; set; }
        [Optional]
        public string Env { get; set; }
    }
}