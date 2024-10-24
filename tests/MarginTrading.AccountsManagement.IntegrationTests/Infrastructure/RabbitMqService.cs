using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Serializers;
using Lykke.RabbitMqBroker.Publisher.Strategies;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.RabbitMqBroker.Subscriber.Deserializers;

using MarginTrading.AccountsManagement.IntegrationTests.Settings;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.PlatformAbstractions;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using RabbitMQ.Client;

namespace MarginTrading.AccountsManagement.IntegrationTests.Infrastructure
{
    public class RabbitMqService : IDisposable
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            Converters = {new StringEnumConverter()}
        };

        private readonly ConcurrentDictionary<string, IStartStop> _subscribers =
            new ConcurrentDictionary<string, IStartStop>();

        private readonly ConcurrentDictionary<RabbitMqSubscriptionSettings, Lazy<IStartStop>> _producers =
            new ConcurrentDictionary<RabbitMqSubscriptionSettings, Lazy<IStartStop>>(
                new SubscriptionSettingsEqualityComparer());

        private readonly ConnectionProvider _connectionProvider = new ConnectionProvider(
            NullLogger<ConnectionProvider>.Instance,
            new AutorecoveringConnectionFactory());

        public void Dispose()
        {
            foreach (var stoppable in _subscribers.Values)
                stoppable.Stop();
            foreach (var stoppable in _producers.Values)
                stoppable.Value.Stop();

            _connectionProvider.Dispose();
        }

        public IRabbitMqSerializer<TMessage> GetJsonSerializer<TMessage>()
        {
            return new JsonMessageSerializer<TMessage>(Encoding.UTF8, JsonSerializerSettings);
        }

        public IRabbitMqSerializer<TMessage> GetMsgPackSerializer<TMessage>()
        {
            return new MessagePackMessageSerializer<TMessage>(options: null);
        }

        public IMessageDeserializer<TMessage> GetJsonDeserializer<TMessage>()
        {
            return new JsonMessageDeserializer<TMessage>(JsonSerializerSettings);
        }

        public IMessageDeserializer<TMessage> GetMsgPackDeserializer<TMessage>()
        {
            return new MessagePackMessageDeserializer<TMessage>();
        }

        public RabbitMqPublisher<TMessage> GetProducer<TMessage>(RabbitConnectionSettings settings,
            bool isDurable, IRabbitMqSerializer<TMessage> serializer)
        {
            var subscriptionSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.ConnectionString,
                ExchangeName = settings.ExchangeName,
                IsDurable = isDurable,
            };

            return (RabbitMqPublisher<TMessage>) _producers.GetOrAdd(subscriptionSettings, CreateProducer).Value;

            Lazy<IStartStop> CreateProducer(RabbitMqSubscriptionSettings s)
            {
                // Lazy ensures RabbitMqPublisher will be created and started only once
                // https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
                return new Lazy<IStartStop>(() =>
                {
                    var publisher = new RabbitMqPublisher<TMessage>(new NullLoggerFactory(), s)
                        .DisableInMemoryQueuePersistence()
                        .SetSerializer(serializer)
                        .SetPublishStrategy(new TopicPublishStrategy(subscriptionSettings.ExchangeName))
                        .PublishSynchronously();
                    publisher.Start();
                    return publisher;
                });
            }
        }

        private class TopicPublishStrategy : IRabbitMqPublishStrategy
        {
            private readonly string _exchangeName;
            
            public TopicPublishStrategy(string exchangeName)
            {
                _exchangeName = exchangeName;
            }
            public void Configure(IModel channel)
            {
                channel.ExchangeDeclare(_exchangeName, "topic", true);
            }

            public void Publish(IModel channel, RawMessage message)
            {
                channel.BasicPublish(_exchangeName, message.RoutingKey, null, message.Body);
            }
        }

        public void Subscribe<TMessage>(RabbitConnectionSettings settings, bool isDurable,
            Func<TMessage, Task> handler, IMessageDeserializer<TMessage> deserializer)
        {
            var subscriptionSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = settings.ConnectionString,
                QueueName =
                    $"{settings.ExchangeName}.{PlatformServices.Default.Application.ApplicationName}.{settings.RoutingKey ?? "all"}",
                ExchangeName = settings.ExchangeName,
                RoutingKey = settings.RoutingKey,
                IsDurable = isDurable,
            };

            var rabbitMqSubscriber = new RabbitMqSubscriber<TMessage>(NullLogger<RabbitMqSubscriber<TMessage>>.Instance, 
                    subscriptionSettings,
                    _connectionProvider.GetExclusive(subscriptionSettings.ConnectionString))
                .SetMessageDeserializer(deserializer)
                .Subscribe(handler);

            if (!_subscribers.TryAdd(subscriptionSettings.QueueName, rabbitMqSubscriber))
            {
                throw new InvalidOperationException(
                    $"A subscriber for queue {subscriptionSettings.QueueName} was already initialized");
            }

            rabbitMqSubscriber.Start();
        }

        /// <remarks>
        ///     ReSharper auto-generated
        /// </remarks>
        private sealed class SubscriptionSettingsEqualityComparer : IEqualityComparer<RabbitMqSubscriptionSettings>
        {
            public bool Equals(RabbitMqSubscriptionSettings x, RabbitMqSubscriptionSettings y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.ConnectionString, y.ConnectionString) &&
                       string.Equals(x.ExchangeName, y.ExchangeName);
            }

            public int GetHashCode(RabbitMqSubscriptionSettings obj)
            {
                unchecked
                {
                    return ((obj.ConnectionString != null ? obj.ConnectionString.GetHashCode() : 0) * 397) ^
                           (obj.ExchangeName != null ? obj.ExchangeName.GetHashCode() : 0);
                }
            }
        }
    }
}