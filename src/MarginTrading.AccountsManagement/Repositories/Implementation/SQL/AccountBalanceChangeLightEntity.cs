// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    public class AccountBalanceChangeLightEntity: IAccountBalanceChangeLight
    {
        public string AccountId { get; set; }
        
        public decimal ChangeAmount { get; set; }

        public decimal Balance { get; set; }

        AccountBalanceChangeReasonType IAccountBalanceChangeLight.ReasonType => Enum.Parse<AccountBalanceChangeReasonType>(ReasonType); 
        public string ReasonType { get; set; }
    }
}