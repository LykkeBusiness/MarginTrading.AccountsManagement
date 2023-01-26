// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Lykke.Middlewares;
using Lykke.Middlewares.Mappers;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Snow.Mdm.Contracts.Models.Events;
using MarginTrading.AccountsManagement.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Workflow.BrokerSettings
{
    public class BrokerSettingsListener : HostedServiceMiddleware, IHostedService
    {
        private readonly ILogger<BrokerSettingsListener> logger;
        private readonly RabbitMqPullingSubscriber<BrokerSettingsChangedEvent> brokerSettingsChangedSubscriber;
        private readonly IBrokerSettingsCache brokerSettingsCache;
        private readonly string brokerId;

        public BrokerSettingsListener(
            ILogger<BrokerSettingsListener> logger,
            RabbitMqPullingSubscriber<BrokerSettingsChangedEvent> brokerSettingsChangedSubscriber,
            IBrokerSettingsCache brokerSettingsCache,
            string brokerId)
            : base(new DefaultLogLevelMapper(), logger)
        {
            this.logger = logger;
            this.brokerSettingsChangedSubscriber = brokerSettingsChangedSubscriber;
            this.brokerSettingsCache = brokerSettingsCache;
            this.brokerId = brokerId;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            brokerSettingsCache.Initialize();
            
            this.brokerSettingsChangedSubscriber
                .Subscribe(@event => this.DecorateAndHandle(() => this.HandleBrokerSettingsChangedEvent(@event), @event.EventId))
                .Start();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.brokerSettingsChangedSubscriber.Stop();

            return Task.CompletedTask;
        }

        private Task HandleBrokerSettingsChangedEvent(BrokerSettingsChangedEvent @event)
        {
            if(@event.ChangeType == ChangeType.Edition && @event.NewValue.BrokerId == brokerId)
                brokerSettingsCache.Update(@event.NewValue);

            return Task.CompletedTask;
        }
    }
}