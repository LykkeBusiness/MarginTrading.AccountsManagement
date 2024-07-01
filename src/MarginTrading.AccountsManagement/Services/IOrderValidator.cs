// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.AccountsManagement.Extensions.AdditionalInfo;
using MarginTrading.Backend.Contracts.Orders;

namespace MarginTrading.AccountsManagement.Services
{
    public interface IOrderValidator
    {
        bool Warning871mConfirmed(OrderContract order) => order.Warning871mConfirmed();
    }
}