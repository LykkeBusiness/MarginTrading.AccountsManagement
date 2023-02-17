// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace MarginTrading.AccountsManagement.Settings
{
    [UsedImplicitly]
    public class RabbitMqSettings
    {
        public SubscriptionSettings OrderHistory { get; set; }
        
        public SubscriptionSettings BrokerSettings { get; set; }
        
        public SubscriptionSettings EodProcessFinished { get; set; }
        
        public SubscriptionSettings LossPercentageUpdated { get; set; }

    }
}