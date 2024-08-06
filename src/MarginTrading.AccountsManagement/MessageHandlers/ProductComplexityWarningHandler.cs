// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using JetBrains.Annotations;

using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Snow.Mdm.Contracts.BrokerFeatures;

using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.Backend.Contracts.Events;

using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.MessageHandlers
{
    [UsedImplicitly]
    internal sealed class ProductComplexityWarningHandler : IMessageHandler<OrderHistoryEvent>
    {
        private readonly IAccountManagementService _accountManagementService;
        private readonly IComplexityWarningRepository _complexityWarningRepository;
        private readonly AccountManagementSettings _settings;
        private readonly ILogger<ProductComplexityWarningHandler> _logger;
        private readonly IComplexityWarningConfiguration _complexityWarningConfiguration;
        private readonly IOrderHistoryValidator _orderHistoryValidator;
        private readonly IOrderValidator _orderValidator;

        public ProductComplexityWarningHandler(
            ILogger<ProductComplexityWarningHandler> logger,
            IAccountManagementService accountManagementService,
            IComplexityWarningRepository complexityWarningRepository,
            AccountManagementSettings settings,
            IComplexityWarningConfiguration complexityWarningConfiguration,
            IOrderHistoryValidator orderHistoryValidator,
            IOrderValidator orderValidator)
        {
            _accountManagementService = accountManagementService;
            _complexityWarningRepository = complexityWarningRepository;
            _settings = settings;
            _complexityWarningConfiguration = complexityWarningConfiguration;
            _orderHistoryValidator = orderHistoryValidator;
            _orderValidator = orderValidator;
            _logger = logger;
        }

        public async Task Handle(OrderHistoryEvent message)
        {
            var featureEnabled = await _complexityWarningConfiguration.IsEnabled;
            if (!featureEnabled)
            {
                return;
            }
            
            if (!_orderHistoryValidator.IsBasicAndPlaceTypeOrder(message))
            {
                return;
            }

            var order = message.OrderSnapshot;

            if (_orderValidator.ProductComplexityConfirmationReceived(order, false))
            {
                _logger.LogInformation(
                    $"Product complexity confirmation received for account {order.AccountId} and orderId {order.Id}");

                var entity = await _complexityWarningRepository.GetOrCreate(
                    order.AccountId,
                    () => ComplexityWarningState.Start(order.AccountId));

                entity.OnConfirmedOrderReceived(
                    order.Id,
                    order.CreatedTimestamp,
                    _settings.ComplexityWarningsCount,
                    out var confirmationFlagShouldBeSwitched);
                
                if (confirmationFlagShouldBeSwitched)
                {
                    await _accountManagementService.UpdateComplexityWarningFlag(
                        order.AccountId,
                        shouldShowProductComplexityWarning: false,
                        order.Id);
                
                    _logger.LogInformation(
                        $"Flag {BrokerFeature.ProductComplexityWarning} for account {entity.AccountId} is switched to off");
                }

                await _complexityWarningRepository.Save(entity);
            }
        }
    }
}