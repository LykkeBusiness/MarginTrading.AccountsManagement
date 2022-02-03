// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.AccountsManagement.InternalModels.Interfaces
{
    public interface IAccountBalanceChangeLight
    {
        string AccountId { get; }

        decimal ChangeAmount { get; }

        decimal Balance { get; }

        AccountBalanceChangeReasonType ReasonType { get; }
        
        DateTime ChangeTimestamp { get; }
    }
}