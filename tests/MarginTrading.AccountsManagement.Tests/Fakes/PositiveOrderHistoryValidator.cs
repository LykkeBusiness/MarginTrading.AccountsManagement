// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.AccountsManagement.Services;
using MarginTrading.Backend.Contracts.Events;

namespace MarginTrading.AccountsManagement.Tests.Fakes
{
    internal sealed class PositiveOrderHistoryValidator : IOrderHistoryValidator
    {
        bool IOrderHistoryValidator.IsBasicAndPlaceTypeOrder(OrderHistoryEvent @event)
        {
            return true;
        }
    }
}