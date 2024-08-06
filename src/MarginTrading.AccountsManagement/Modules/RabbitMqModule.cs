// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using Autofac;

using BookKeeper.Client.Workflow.Events;

using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.RabbitMqBroker.Subscriber.Middleware.ErrorHandling;
using Lykke.Snow.Common.Correlation.RabbitMq;
using Lykke.Snow.Mdm.Contracts.Models.Events;

using MarginTrading.AccountsManagement.MessageHandlers;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.Backend.Contracts.Events;

using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Modules
{
    public class RabbitMqModule : Module
    {
        private readonly RabbitMqSettings _settings;

        public RabbitMqModule(RabbitMqSettings settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.AddRabbitMqConnectionProvider();

            builder.AddRabbitMqListener<BrokerSettingsChangedEvent, BrokerSettingsHandler>(
                    _settings.BrokerSettings,
                    ConfigureBrokerSettingsSubscriber)
                .AddOptions(RabbitMqListenerOptions<BrokerSettingsChangedEvent>.MessagePack.NoLoss);
            
            builder.AddRabbitMqListener<EodProcessFinishedEvent, EodFinishedHandler>(
                    _settings.EodProcessFinished,
                    ConfigureEodSubscriber)
                .AddOptions(RabbitMqListenerOptions<EodProcessFinishedEvent>.MessagePack.NoLoss);
            
            builder.AddRabbitMqListener<OrderHistoryEvent, Warning871Handler>(
                    _settings.OrderHistory, 
                    ConfigureOrderHistorySubscriber)
                .AddMessageHandler<ProductComplexityWarningHandler>()
                .AddOptions(RabbitMqListenerOptions<OrderHistoryEvent>.Json.NoLoss);
        }
        
        private static void ConfigureOrderHistorySubscriber(
            RabbitMqSubscriber<OrderHistoryEvent> subscriber,
            IComponentContext сtx)
        {
            var loggerFactory = сtx.Resolve<ILoggerFactory>();
            subscriber.UseMiddleware(
                new ResilientErrorHandlingMiddleware<OrderHistoryEvent>(
                    loggerFactory.CreateLogger<ResilientErrorHandlingMiddleware<OrderHistoryEvent>>(),
                    TimeSpan.FromSeconds(1)));
            
            var correlationManager = сtx.Resolve<RabbitMqCorrelationManager>();
            subscriber.SetReadHeadersAction(correlationManager.FetchCorrelationIfExists);
        }
        
        private static void ConfigureEodSubscriber(
            RabbitMqSubscriber<EodProcessFinishedEvent> subscriber,
            IComponentContext ctx)
        {
            var correlationManager = ctx.Resolve<RabbitMqCorrelationManager>();
            subscriber.SetReadHeadersAction(correlationManager.FetchCorrelationIfExists);
        }
        
        private static void ConfigureBrokerSettingsSubscriber(
            RabbitMqSubscriber<BrokerSettingsChangedEvent> subscriber,
            IComponentContext ctx)
        {
            var correlationManager = ctx.Resolve<RabbitMqCorrelationManager>();
            subscriber.SetReadHeadersAction(correlationManager.FetchCorrelationIfExists);
        }
    }
}