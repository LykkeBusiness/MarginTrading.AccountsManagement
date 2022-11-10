// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;

using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Strategies;

using RabbitMQ.Client;

namespace MarginTrading.AccountsManagement.RabbitMq
{
    public sealed class TopicPublishingStrategy : IRabbitMqPublishStrategy
    {
        private readonly bool durable;
        private readonly string routingKey;

        public TopicPublishingStrategy(RabbitMqSubscriptionSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            this.routingKey = settings.RoutingKey ?? string.Empty;
            this.durable = settings.IsDurable;
        }

        public void Configure(RabbitMqSubscriptionSettings settings, IModel channel)
        {
            channel.ExchangeDeclare(exchange: settings.ExchangeName, type: "topic", durable: this.durable);
        }

        public void Publish(RabbitMqSubscriptionSettings settings, IModel channel, RawMessage message)
        {
            IBasicProperties properties = channel.CreateBasicProperties();
            properties.DeliveryMode = 2;
            properties.Type = settings.RoutingKey;
            var headers = message.Headers ?? new Dictionary<string, object>();
            headers.Add("initialRoute", string.Format(CultureInfo.InvariantCulture, @"topic://{0}/{1}", settings.ExchangeName, settings.RoutingKey));
            properties.Headers = new Dictionary<string, object>(headers);

            channel.BasicPublish(
                exchange: settings.ExchangeName,
                routingKey: message.RoutingKey ?? this.routingKey,
                basicProperties: properties,
                body: message.Body);
        }
    }
}