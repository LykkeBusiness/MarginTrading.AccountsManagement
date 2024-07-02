// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.AccountsManagement.Extensions;
using MarginTrading.Backend.Contracts.Events;

namespace MarginTrading.AccountsManagement.Services
{
    public interface IOrderHistoryValidator
    {
        bool IsBasicAndPlaceTypeOrder(OrderHistoryEvent @event) => @event.IsBasicAndPlaceTypeOrder();
    }
}