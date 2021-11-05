// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.AccountsManagement.Settings
{
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

        public static implicit operator RabbitMqSubscriptionSettings(SubscriptionSettings donutSubscriptionSettings)
        {
            return new RabbitMqSubscriptionSettings()
            {
                RoutingKey = donutSubscriptionSettings.RoutingKey,
                IsDurable = donutSubscriptionSettings.IsDurable,
                ExchangeName = donutSubscriptionSettings.ExchangeName,
                QueueName = donutSubscriptionSettings.QueueName,
                ConnectionString = donutSubscriptionSettings.ConnectionString
            };
        }
    }
}