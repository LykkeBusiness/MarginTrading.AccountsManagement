// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    public class ClientWithAccountsEntity : IClientWithAccounts
    {
        public string Id { get; set; }
        public string TradingConditionId { get; set; }
        public string AccountIdentityCommaSeparatedList { get; set; }
        public string UserId { get; set; }
        public DateTime ModificationTimestamp { get; set; }
    }
}