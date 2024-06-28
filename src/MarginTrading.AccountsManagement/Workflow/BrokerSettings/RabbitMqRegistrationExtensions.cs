// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using BookKeeper.Client.Workflow.Events;

using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.RabbitMqBroker.Subscriber.Middleware.ErrorHandling;
using Lykke.Snow.Common.Correlation.RabbitMq;
using Lykke.Snow.Mdm.Contracts.Models.Events;

using MarginTrading.AccountsManagement.MessageHandlers;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.Backend.Contracts.Events;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Workflow.BrokerSettings
{
    internal static class RabbitMqRegistrationExtensions
    {
        public static void AddRabbitMqListeners(this IServiceCollection services, AppSettings settings)
        {
            services.AddRabbitMqConnectionProvider();
            
            services.AddRabbitMqListener<BrokerSettingsChangedEvent, BrokerSettingsHandler>(
                    settings.MarginTradingAccountManagement.RabbitMq.BrokerSettings,
                    ConfigureBrokerSettingsSubscriber)
                .AddOptions(RabbitMqListenerOptions<BrokerSettingsChangedEvent>.MessagePack.NoLoss);
            
            services.AddRabbitMqListener<EodProcessFinishedEvent, EodFinishedHandler>(
                    settings.MarginTradingAccountManagement.RabbitMq.EodProcessFinished,
                    ConfigureEodSubscriber)
                .AddOptions(RabbitMqListenerOptions<EodProcessFinishedEvent>.MessagePack.NoLoss);
            
            services.AddRabbitMqListener<OrderHistoryEvent, Warning871Handler>(
                    settings.MarginTradingAccountManagement.RabbitMq.OrderHistory, 
                    ConfigureOrderHistorySubscriber)
                .AddMessageHandler<ProductComplexityWarningHandler>()
                .AddOptions(RabbitMqListenerOptions<OrderHistoryEvent>.Json.NoLoss);
        }

        private static void ConfigureOrderHistorySubscriber(
            RabbitMqSubscriber<OrderHistoryEvent> subscriber,
            IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            subscriber.UseMiddleware(
                new ResilientErrorHandlingMiddleware<OrderHistoryEvent>(
                    loggerFactory.CreateLogger<ResilientErrorHandlingMiddleware<OrderHistoryEvent>>(),
                    TimeSpan.FromSeconds(1)));
            
            var correlationManager = serviceProvider.GetRequiredService<RabbitMqCorrelationManager>();
            subscriber.SetReadHeadersAction(correlationManager.FetchCorrelationIfExists);
        }
        
        private static void ConfigureEodSubscriber(
            RabbitMqSubscriber<EodProcessFinishedEvent> subscriber,
            IServiceProvider serviceProvider)
        {
            var correlationManager = serviceProvider.GetRequiredService<RabbitMqCorrelationManager>();
            subscriber.SetReadHeadersAction(correlationManager.FetchCorrelationIfExists);
        }
        
        private static void ConfigureBrokerSettingsSubscriber(
            RabbitMqSubscriber<BrokerSettingsChangedEvent> subscriber,
            IServiceProvider serviceProvider)
        {
            var correlationManager = serviceProvider.GetRequiredService<RabbitMqCorrelationManager>();
            subscriber.SetReadHeadersAction(correlationManager.FetchCorrelationIfExists);
        }
    }
}