// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.AccountsManagement.Services;
using MarginTrading.Backend.Contracts.Orders;

namespace MarginTrading.AccountsManagement.Tests.Fakes
{
    internal sealed class NegativeOrderValidator : IOrderValidator
    {
        bool IOrderValidator.Warning871mConfirmed(OrderContract order)
        {
            return false;
        }
        
        bool IOrderValidator.ProductComplexityConfirmationReceived(OrderContract order, bool defaultValue)
        {
            return false;
        }
    }
}