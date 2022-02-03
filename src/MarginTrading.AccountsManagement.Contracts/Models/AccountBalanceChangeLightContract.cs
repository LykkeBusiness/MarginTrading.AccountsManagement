// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.AccountsManagement.Contracts.Models
{
    public class AccountBalanceChangeLightContract
    {
        public decimal ChangeAmount { get; }
        public decimal Balance { get; }
        public AccountBalanceChangeReasonTypeContract ReasonType { get; }
        public DateTime ChangeTimestamp { get; }

        public AccountBalanceChangeLightContract(decimal changeAmount, decimal balance, AccountBalanceChangeReasonTypeContract reasonType, DateTime changeTimestamp)
        {
            ChangeAmount = changeAmount;
            Balance = balance;
            ReasonType = reasonType;
            ChangeTimestamp = changeTimestamp;
        }
    }
}