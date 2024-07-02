// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using JetBrains.Annotations;

using Lykke.RabbitMqBroker.Subscriber;

using MarginTrading.AccountsManagement.Contracts.Models.AdditionalInfo;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.Backend.Contracts.Events;

using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.MessageHandlers
{
    [UsedImplicitly]
    internal sealed class Warning871Handler : IMessageHandler<OrderHistoryEvent>
    {
        private readonly IAccountManagementService _accountManagementService;
        private readonly IOrderHistoryValidator _orderHistoryValidator;
        private readonly IOrderValidator _orderValidator;
        private readonly ILogger<Warning871Handler> _logger;

        public Warning871Handler(
            ILogger<Warning871Handler> logger,
            IAccountManagementService accountManagementService,
            IOrderHistoryValidator orderHistoryValidator,
            IOrderValidator orderValidator)
        {
            _logger = logger;
            _accountManagementService = accountManagementService;
            _orderHistoryValidator = orderHistoryValidator;
            _orderValidator = orderValidator;
        }

        public async Task Handle(OrderHistoryEvent message)
        {
            if (!_orderHistoryValidator.IsBasicAndPlaceTypeOrder(message))
            {
                return;
            }

            var order = message.OrderSnapshot;

            if (_orderValidator.Warning871mConfirmed(order))
            {
                _logger.LogInformation(
                    $"871m warning confirmation received for account {order.AccountId} and orderId {order.Id}");

                await _accountManagementService.Update871mWarningFlag(
                    order.AccountId,
                    shouldShow871mWarning: false,
                    order.Id);

                _logger.LogInformation(
                    $"Flag {nameof(AccountAdditionalInfo.ShouldShow871mWarning)} for account {order.AccountId} is switched to off");
            }
        }
    }
}