using System;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Logs;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.RabbitMqBroker.Subscriber.Deserializers;
using Lykke.RabbitMqBroker.Subscriber.MessageReadStrategies;
using Lykke.RabbitMqBroker.Subscriber.Middleware.ErrorHandling;
using Lykke.Snow.Common.Correlation.RabbitMq;
using Lykke.Snow.Common.Startup;
using Lykke.Snow.Mdm.Contracts.Models.Events;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AccountsManagement.Workflow.BrokerSettings;
using MarginTrading.Backend.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Workflow.ProductComplexity
{
    internal static class ServiceCollectionExtensions
    {
        public static void AddProductComplexity(this IServiceCollection services, AppSettings settings)
        {
            services.AddHostedService<CleanupExpiredComplexityService>();
            services.AddHostedService<OrderHistoryListener>();
            services.AddHostedService(x => ActivatorUtilities.CreateInstance<BrokerSettingsListener>(
                x,
                settings.MarginTradingAccountManagement.BrokerId));
            
            services.AddSingleton(ctx => new RabbitMqSubscriber<OrderHistoryEvent>(
                    ctx.GetRequiredService<ILoggerFactory>().CreateLogger<RabbitMqSubscriber<OrderHistoryEvent>>(),
                    settings.MarginTradingAccountManagement.RabbitMq.OrderHistory)
                .SetMessageDeserializer(new JsonMessageDeserializer<OrderHistoryEvent>())
                .SetMessageReadStrategy(new MessageReadQueueStrategy())
                .UseMiddleware(new ExceptionSwallowMiddleware<OrderHistoryEvent>(
                    ctx.GetRequiredService<ILoggerFactory>().CreateLogger<ExceptionSwallowMiddleware<OrderHistoryEvent>>()))
                .UseMiddleware(new ResilientErrorHandlingMiddleware<OrderHistoryEvent>(
                    ctx.GetRequiredService<ILoggerFactory>().CreateLogger<ResilientErrorHandlingMiddleware<OrderHistoryEvent>>(),
                    TimeSpan.FromSeconds(1)))
                .SetReadHeadersAction(ctx.GetRequiredService<RabbitMqCorrelationManager>().FetchCorrelationIfExists)
                .CreateDefaultBinding());
        }
    }
}
