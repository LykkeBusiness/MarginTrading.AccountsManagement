// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using JetBrains.Annotations;

using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Snow.Common.Startup;
using Lykke.Snow.Mdm.Contracts.Models.Events;

using MarginTrading.AccountsManagement.Services;

namespace MarginTrading.AccountsManagement.MessageHandlers
{
    [UsedImplicitly]
    internal sealed class BrokerSettingsHandler : IMessageHandler<BrokerSettingsChangedEvent>
    {
        private readonly IBrokerSettingsCache _brokerSettingsCache;
        private readonly BrokerId _brokerId;

        public BrokerSettingsHandler(IBrokerSettingsCache brokerSettingsCache, BrokerId brokerId)
        {
            _brokerSettingsCache = brokerSettingsCache;
            _brokerId = brokerId;
        }

        public Task Handle(BrokerSettingsChangedEvent message)
        {
            if(message.ChangeType == ChangeType.Edition && message.NewValue.BrokerId == _brokerId)
                _brokerSettingsCache.Update(message.NewValue);

            return Task.CompletedTask;
        }
    }
}