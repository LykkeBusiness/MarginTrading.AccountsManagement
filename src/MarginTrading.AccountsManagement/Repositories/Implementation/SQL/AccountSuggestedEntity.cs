// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.AccountsManagement.InternalModels.Interfaces;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    public class AccountSuggestedEntity: IAccountSuggested
    {
        public string Id { get; set; }
        public string AccountName { get; set; }
    }
}