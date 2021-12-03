// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MarginTrading.AccountsManagement.InternalModels.Interfaces
{
    public interface IClientSearchResult : IClient
    {
        /// <summary>
        /// Either account id or account name comma separated list
        /// </summary>
        string AccountIdentityCommaSeparatedList { get; set; }
        
        /// <summary>
        /// The user identifier which was generated and passed by broker 
        /// </summary>
        string UserId { get; set; }
    }
}