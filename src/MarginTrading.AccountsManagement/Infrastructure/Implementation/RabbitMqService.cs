// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Serializers;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.RabbitMqBroker.Subscriber.Deserializers;
using Lykke.RabbitMqBroker.Subscriber.Middleware.ErrorHandling;
using Lykke.SettingsReader;
using Lykke.Snow.Common.Correlation.RabbitMq;
using MarginTrading.AccountsManagement.Settings;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.AccountsManagement.Infrastructure.Implementation
{
    public class RabbitMqService : IRabbitMqService, IDisposable
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            Converters = {new StringEnumConverter()}
        };

        private readonly ILoggerFactory _loggerFactory;
        private readonly RabbitMqCorrelationManager _correlationManager;

        private readonly ConcurrentDictionary<string, IStartStop> _subscribers =
            new ConcurrentDictionary<string, IStartStop>();

        private readonly ConcurrentDictionary<RabbitMqSubscriptionSettings, Lazy<IStartStop>> _producers =
            new ConcurrentDictionary<RabbitMqSubscriptionSettings, Lazy<IStartStop>>(
                new SubscriptionSettingsEqualityComparer());

        public RabbitMqService(ILoggerFactory loggerFactory, RabbitMqCorrelationManager correlationManager)
        {
            _loggerFactory = loggerFactory;
            _correlationManager = correlationManager;
        }

        public void Dispose()
        {
            foreach (var stoppable in _subscribers.Values)
                stoppable.Stop();
            foreach (var stoppable in _producers.Values)
                stoppable.Value.Stop();
        }

        public IRabbitMqSerializer<TMessage> GetJsonSerializer<TMessage>()
        {
            return new JsonMessageSerializer<TMessage>(Encoding.UTF8, JsonSerializerSettings);
        }

        public IRabbitMqSerializer<TMessage> GetMsgPackSerializer<TMessage>()
        {
            return new MessagePackMessageSerializer<TMessage>();
        }

        public Common.IMessageProducer<TMessage> GetProducer<TMessage>(IReloadingManager<RabbitConnectionSettings> settings,
            bool isDurable, IRabbitMqSerializer<TMessage> serializer)
        {
            // on-the fly connection strings switch is not supported currently for rabbitMq
            var currSettings = settings.CurrentValue;
            var subscriptionSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = currSettings.ConnectionString,
                ExchangeName = currSettings.ExchangeName,
                IsDurable = isDurable,
            };

            return (Common.IMessageProducer<TMessage>) _producers.GetOrAdd(subscriptionSettings, CreateProducer).Value;

            Lazy<IStartStop> CreateProducer(RabbitMqSubscriptionSettings s)
            {
                // Lazy ensures RabbitMqPublisher will be created and started only once
                // https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
                return new Lazy<IStartStop>(() =>
                {
                    var result = new RabbitMqPublisher<TMessage>(_loggerFactory, s).DisableInMemoryQueuePersistence()
                        .SetSerializer(serializer)
                        .SetWriteHeadersFunc(_correlationManager.BuildCorrelationHeadersIfExists);
                    result.Start();
                    return result;
                });
            }
        }

        public void Subscribe<TMessage>(IReloadingManager<RabbitConnectionSettings> settings, bool isDurable,
            Func<TMessage, Task> handler)
        {
            // on-the fly connection strings switch is not supported currently for rabbitMq
            var currSettings = settings.CurrentValue;
            var subscriptionSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = currSettings.ConnectionString,
                QueueName =
                    $"{currSettings.ExchangeName}.{PlatformServices.Default.Application.ApplicationName}",
                ExchangeName = currSettings.ExchangeName,
                IsDurable = isDurable,
            };

            var rabbitMqSubscriber = new RabbitMqSubscriber<TMessage>(
                    _loggerFactory.CreateLogger<RabbitMqSubscriber<TMessage>>(),
                    subscriptionSettings)
                .SetMessageDeserializer(new JsonMessageDeserializer<TMessage>(JsonSerializerSettings))
                .Subscribe(handler)
                .UseMiddleware(new ExceptionSwallowMiddleware<TMessage>(
                    _loggerFactory.CreateLogger<ExceptionSwallowMiddleware<TMessage>>()))
                .SetReadHeadersAction(_correlationManager.FetchCorrelationIfExists);

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