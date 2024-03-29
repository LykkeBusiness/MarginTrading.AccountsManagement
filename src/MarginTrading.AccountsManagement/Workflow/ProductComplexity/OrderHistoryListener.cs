﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Middlewares;
using Lykke.Middlewares.Mappers;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Snow.Mdm.Contracts.BrokerFeatures;

using MarginTrading.AccountsManagement.Contracts.Models.AdditionalInfo;
using MarginTrading.AccountsManagement.Extensions.AdditionalInfo;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Orders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using BrokerFeature = Lykke.Snow.Mdm.Contracts.BrokerFeatures.BrokerFeature;

namespace MarginTrading.AccountsManagement.Workflow.ProductComplexity
{
    public class OrderHistoryListener : HostedServiceMiddleware, IHostedService
    {
        private readonly IComplexityWarningRepository _complexityWarningRepository;
        private readonly IAccountManagementService _accountManagementService;
        private readonly IFeatureManager _featureManager;
        private readonly AccountManagementSettings _settings;
        private readonly ILogger<OrderHistoryListener> _log;
        private readonly RabbitMqPullingSubscriber<OrderHistoryEvent> _subscriber;

        public OrderHistoryListener(IComplexityWarningRepository complexityWarningRepository, 
            IAccountManagementService accountManagementService,
            IFeatureManager featureManager,
            AccountManagementSettings settings, 
            ILogger<OrderHistoryListener> log,
            RabbitMqPullingSubscriber<OrderHistoryEvent> subscriber):base(new DefaultLogLevelMapper(), log)
        {
            _complexityWarningRepository = complexityWarningRepository;
            _accountManagementService = accountManagementService;
            _featureManager = featureManager;
            _settings = settings;
            _log = log;
            _subscriber = subscriber;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var handlers = new List<Func<OrderHistoryEvent, Task>>();
            
            if (await _featureManager.IsEnabledAsync(BrokerFeature.ProductComplexityWarning))
            {
                if (_settings.ComplexityWarningsCount <= 0) 
                {
                    throw new InvalidOperationException($"Broker {_settings.BrokerId} feature {BrokerFeature.ProductComplexityWarning} is enabled, " +
                                                        $"but {nameof(_settings.ComplexityWarningsCount)} = {_settings.ComplexityWarningsCount} is not valid ");
                }
                handlers.Add(HandleProductComplexityWarning);
            }
            
            handlers.Add(Handle871mWarning);
            
            _subscriber
                .Subscribe(e => this.Handle(e, handlers))
                .Start();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _subscriber.Stop();

            return Task.CompletedTask;
        }

        private async Task HandleProductComplexityWarning(OrderHistoryEvent e)
        {
            if (!IsBasicAndPlaceTypeOrder(e))
            {
                return;
            }

            var order = e.OrderSnapshot;
            
            if (order.ProductComplexityConfirmationReceived())
            {
                _log.LogInformation($"Product complexity confirmation received for account {order.AccountId} and orderId {order.Id}");

                var entity = await _complexityWarningRepository.GetOrCreate(order.AccountId,
                    () => ComplexityWarningState.Start(order.AccountId));

                entity.OnConfirmedOrderReceived(order.Id, order.CreatedTimestamp, _settings.ComplexityWarningsCount, out var confirmationFlagShouldBeSwitched);

                if (confirmationFlagShouldBeSwitched)
                {
                    await _accountManagementService.UpdateComplexityWarningFlag(order.AccountId, shouldShowProductComplexityWarning: false, order.Id);

                    _log.LogInformation($"Flag {BrokerFeature.ProductComplexityWarning} for account {entity.AccountId} is switched to off");
                }

                await _complexityWarningRepository.Save(entity);   
            }
        }

        private async Task Handle871mWarning(OrderHistoryEvent e)
        {
            if (!IsBasicAndPlaceTypeOrder(e))
            {
                return;
            }

            var order = e.OrderSnapshot;
            
            if (order.Warning871mConfirmed())
            {
                _log.LogInformation($"871m warning confirmation received for account {order.AccountId} and orderId {order.Id}");
                
                await _accountManagementService.Update871mWarningFlag(order.AccountId, shouldShow871mWarning: false, order.Id);

                _log.LogInformation($"Flag {nameof(AccountAdditionalInfo.ShouldShow871mWarning)} for account {order.AccountId} is switched to off"); 
            }
        }

        private bool IsBasicAndPlaceTypeOrder(OrderHistoryEvent e)
        {
            var order = e.OrderSnapshot;
            var isBasicOrder = new[]
                {
                    OrderTypeContract.Market,
                    OrderTypeContract.Limit,
                    OrderTypeContract.Stop
                }
                .Contains(order.Type);

            return isBasicOrder && e.Type == OrderHistoryTypeContract.Place;
        }

        private async Task Handle(OrderHistoryEvent e, List<Func<OrderHistoryEvent, Task>> handlers)
        {
            foreach (var handler in handlers)
            {
                await handler(e);
            }
        }
    }
}
