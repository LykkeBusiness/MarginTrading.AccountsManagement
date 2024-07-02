// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;

using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Orders;

namespace MarginTrading.AccountsManagement.Extensions
{
    internal static class OrderHistoryEventExtensions
    {
        public static bool IsBasicAndPlaceTypeOrder(this OrderHistoryEvent @event)
        {
            var order = @event.OrderSnapshot;
            var isBasicOrder = new[]
                {
                    OrderTypeContract.Market,
                    OrderTypeContract.Limit,
                    OrderTypeContract.Stop
                }
                .Contains(order.Type);

            return isBasicOrder && @event.Type == OrderHistoryTypeContract.Place;
        }
    }
}