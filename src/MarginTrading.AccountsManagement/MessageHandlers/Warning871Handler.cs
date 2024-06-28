// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using JetBrains.Annotations;

using Lykke.RabbitMqBroker.Subscriber;

using MarginTrading.AccountsManagement.Contracts.Models.AdditionalInfo;
using MarginTrading.AccountsManagement.Extensions;
using MarginTrading.AccountsManagement.Extensions.AdditionalInfo;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.Backend.Contracts.Events;

using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.MessageHandlers
{
    [UsedImplicitly]
    internal sealed class Warning871Handler : IMessageHandler<OrderHistoryEvent>
    {
        private readonly IAccountManagementService _accountManagementService;
        private readonly ILogger<Warning871Handler> _logger;

        public Warning871Handler(ILogger<Warning871Handler> logger, IAccountManagementService accountManagementService)
        {
            _logger = logger;
            _accountManagementService = accountManagementService;
        }

        public async Task Handle(OrderHistoryEvent message)
        {
            if (!message.IsBasicAndPlaceTypeOrder())
            {
                return;
            }

            var order = message.OrderSnapshot;

            if (order.Warning871mConfirmed())
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