// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MarginTrading.AccountsManagement.Contracts.Models
{
    public class ClientTradingConditionsSearchResultContract
    {
        public string ClientId { get; set; }

        public string TradingConditionId { get; set; }
        
        public List<string> AccountIdentities { get; set; }
    }
}