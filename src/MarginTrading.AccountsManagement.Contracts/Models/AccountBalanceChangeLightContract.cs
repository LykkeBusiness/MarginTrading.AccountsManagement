// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.AccountsManagement.Contracts.Models
{
    public class AccountBalanceChangeLightContract
    {
        public decimal ChangeAmount { get; }
        public decimal Balance { get; }
        public AccountBalanceChangeReasonTypeContract ReasonType { get; }

        public AccountBalanceChangeLightContract(decimal changeAmount, decimal balance, AccountBalanceChangeReasonTypeContract reasonType)
        {
            ChangeAmount = changeAmount;
            Balance = balance;
            ReasonType = reasonType;
        }
    }
}