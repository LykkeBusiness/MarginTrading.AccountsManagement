// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using BookKeeper.Client.Workflow.Events;

using Lykke.RabbitMqBroker.Subscriber;
using Lykke.RabbitMqBroker.Subscriber.Deserializers;
using Lykke.RabbitMqBroker.Subscriber.MessageReadStrategies;
using Lykke.RabbitMqBroker.Subscriber.Middleware.ErrorHandling;
using Lykke.Snow.Common.Correlation.RabbitMq;
using Lykke.Snow.Mdm.Contracts.Models.Events;

using MarginTrading.AccountsManagement.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Workflow.BrokerSettings
{
    internal static class ServiceCollectionExtensions
    {
        public static void AddBrokerSettings(this IServiceCollection services, AppSettings settings)
        {
            services.AddSingleton(ctx =>
                new RabbitMqSubscriber<BrokerSettingsChangedEvent>(
                        ctx.GetRequiredService<ILoggerFactory>().CreateLogger<RabbitMqSubscriber<BrokerSettingsChangedEvent>>(),
                        settings.MarginTradingAccountManagement.RabbitMq.BrokerSettings)
                    .SetMessageDeserializer(new MessagePackMessageDeserializer<BrokerSettingsChangedEvent>())
                    .SetMessageReadStrategy(new MessageReadQueueStrategy())
                    .UseMiddleware(new ExceptionSwallowMiddleware<BrokerSettingsChangedEvent>(
                        ctx.GetRequiredService<ILoggerFactory>().CreateLogger<ExceptionSwallowMiddleware<BrokerSettingsChangedEvent>>()))
                    .SetReadHeadersAction(ctx.GetRequiredService<RabbitMqCorrelationManager>().FetchCorrelationIfExists)
                    .CreateDefaultBinding());
        }
        
        public static void AddEodProcessFinishedSubscriber(this IServiceCollection services, AppSettings settings)
        {
            services.AddSingleton(ctx =>
                new RabbitMqSubscriber<EodProcessFinishedEvent>(
                        ctx.GetRequiredService<ILoggerFactory>().CreateLogger<RabbitMqSubscriber<EodProcessFinishedEvent>>(),
                        settings.MarginTradingAccountManagement.RabbitMq.EodProcessFinished)
                    .SetMessageDeserializer(new MessagePackMessageDeserializer<EodProcessFinishedEvent>())
                    .SetMessageReadStrategy(new MessageReadQueueStrategy())
                    .UseMiddleware(new ExceptionSwallowMiddleware<EodProcessFinishedEvent>(
                        ctx.GetRequiredService<ILoggerFactory>().CreateLogger<ExceptionSwallowMiddleware<EodProcessFinishedEvent>>()))
                    .SetReadHeadersAction(ctx.GetRequiredService<RabbitMqCorrelationManager>().FetchCorrelationIfExists)
                    .CreateDefaultBinding());
        }
    }
}