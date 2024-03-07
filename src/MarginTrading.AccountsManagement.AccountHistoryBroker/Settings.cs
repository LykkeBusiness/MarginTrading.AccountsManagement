// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Lykke.MarginTrading.BrokerBase.Settings;
using Lykke.RabbitMqBroker;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.AccountsManagement.AccountHistoryBroker
{
    [UsedImplicitly]
    public class Settings : BrokerSettingsBase
    {
        public Db Db { get; set; }
        
        public RabbitMqQueues RabbitMqQueues { get; set; }

        public ServiceSettings AccountManagement { get; set; }
        public RabbitMqSettings RabbitMq { get; set; }
        
        [Optional]
        public ExtendedLoggingSettings ExtendedLoggingSettings { get; set; }
    }
    
    [UsedImplicitly]
    public class Db
    {
        public string StorageMode { get; set; }
        
        public string ConnString { get; set; }
    }
    
    [UsedImplicitly]
    public class RabbitMqQueues
    {
        public RabbitMqQueueInfo AccountHistory { get; set; }
    }

    [UsedImplicitly]
    public class RabbitMqQueueInfo
    {
        public string ExchangeName { get; set; }
    }

    public class ServiceSettings
    {
        [HttpCheck("api/isalive")]
        public string ServiceUrl { get; set; }

        [Optional]
        public string ApiKey { get; set; }
    }

    public class RabbitMqSettings
    {
        public SubscriptionSettings AccountTaxHistoryUpdated { get; set; }
    }
    
    public class ExtendedLoggingSettings
    {
        /// <summary>
        /// Enables detailed logging for the Taxes Saga
        /// </summary>
        [Optional]
        public bool TaxesLoggingEnabled { get; set; }
    }

    public class SubscriptionSettings
    {
        [Optional]
        public string RoutingKey { get; set; }
        [Optional]
        public bool IsDurable { get; set; }

        public string ExchangeName { get; set; }

        [Optional]
        public string QueueName { get; set; }

        public string ConnectionString { get; set; }
        
        [Optional]
        public int NumberOfConsumers { get; set; } = 1;

        public static implicit operator RabbitMqSubscriptionSettings(SubscriptionSettings subscriptionSettings)
        {
            return new RabbitMqSubscriptionSettings()
            {
                RoutingKey = subscriptionSettings.RoutingKey,
                IsDurable = subscriptionSettings.IsDurable,
                ExchangeName = subscriptionSettings.ExchangeName,
                QueueName = subscriptionSettings.QueueName,
                ConnectionString = subscriptionSettings.ConnectionString
            };
        }
    }
}