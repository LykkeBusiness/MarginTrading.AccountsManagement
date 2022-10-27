// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lykke.RabbitMqBroker.Publisher.Serializers;
using Lykke.SettingsReader;
using MarginTrading.AccountsManagement.Settings;

namespace MarginTrading.AccountsManagement.Infrastructure
{
    public interface IRabbitMqService
    {
        Common.IMessageProducer<TMessage> GetProducer<TMessage>(IReloadingManager<RabbitConnectionSettings> settings,
            bool isDurable, IRabbitMqSerializer<TMessage> serializer);

        void Subscribe<TMessage>(IReloadingManager<RabbitConnectionSettings> settings, bool isDurable,
            Func<TMessage, Task> handler);

        IRabbitMqSerializer<TMessage> GetJsonSerializer<TMessage>();
        IRabbitMqSerializer<TMessage> GetMsgPackSerializer<TMessage>();
    }
}