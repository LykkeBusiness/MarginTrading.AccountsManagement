using System;
using System.Collections.Generic;
using System.Globalization;

using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Strategies;

using RabbitMQ.Client;

namespace MarginTrading.AccountsManagement.AccountHistoryBroker
{
    public sealed class PropertiesWithMessageTypeTopicPublishStrategy : IRabbitMqPublishStrategy
    {
        private readonly bool _durable;
        private readonly string _routingKey;
        private readonly string _exchangeName;

        public PropertiesWithMessageTypeTopicPublishStrategy(RabbitMqSubscriptionSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _routingKey = settings.RoutingKey ?? string.Empty;
            _durable = settings.IsDurable;
            _exchangeName = settings.ExchangeName;
        }

        public void Configure(IModel channel)
        {
            channel.ExchangeDeclare(_exchangeName, "topic", _durable);
        }

        public void Publish(IModel channel, RawMessage message)
        {
            IBasicProperties properties = channel.CreateBasicProperties();
            properties.DeliveryMode = 2;
            properties.Type = _routingKey;
            var headers = message.Headers ?? new Dictionary<string, object>();
            headers.Add("initialRoute", string.Format(CultureInfo.InvariantCulture, @"topic://{0}/{1}", _exchangeName, _routingKey));
            properties.Headers = new Dictionary<string, object>(headers);

            channel.BasicPublish(
                _exchangeName,
                message.RoutingKey ?? _routingKey,
                properties,
                message.Body);
        }
    }
}