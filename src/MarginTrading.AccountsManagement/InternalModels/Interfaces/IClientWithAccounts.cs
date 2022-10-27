// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.AccountsManagement.InternalModels.Interfaces
{
    public interface IClientWithAccounts : IClient
    {
        private const char Separator = ',';
        
        /// <summary>
        /// Either account id or account name comma separated list
        /// </summary>
        string AccountIdentityCommaSeparatedList { get; }

        IEnumerable<string> DeserializeAccounts() =>
            AccountIdentityCommaSeparatedList?.Split(Separator) ?? Enumerable.Empty<string>();
    }
}