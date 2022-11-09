// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using MarginTrading.AccountsManagement.RecoveryTool.Model;

using Newtonsoft.Json;

namespace MarginTrading.AccountsManagement.RecoveryTool.Mappers

{
    public class UpdateBalanceInternalCommandMapper
    {
        public UpdateBalanceInternalCommand Map(DomainEvent domainEvent)
        {
            if (string.IsNullOrEmpty(domainEvent?.Json))
                throw new ArgumentNullException(nameof(domainEvent));

            var @event = JsonConvert.DeserializeObject<UpdateBalanceInternalCommand>(domainEvent.Json);

            return @event;
        }
    }
}