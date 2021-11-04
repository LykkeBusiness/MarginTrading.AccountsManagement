// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MarginTrading.AccountsManagement.Contracts.Models
{
    /// <summary>
    /// The result of client search
    /// </summary>
    public class ClientTradingConditionsSearchResultContract
    {
        /// <summary>
        /// The client id
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// The trading condition (client profile) id
        /// </summary>
        public string TradingConditionId { get; set; }
        
        /// <summary>
        /// The broker-provided user identifier
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// The list of account names or id's (if name is empty)
        /// </summary>
        public List<string> AccountIdentities { get; set; }
    }
}