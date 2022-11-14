// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.RecoveryTool.Model;

using Newtonsoft.Json;

namespace MarginTrading.AccountsManagement.RecoveryTool.Mappers
{
    public class ChangeBalanceCommandMapper
    {
        public ChangeBalanceCommand Map(DomainEvent domainEvent)
        {
            if (string.IsNullOrEmpty(domainEvent?.Json))
                throw new ArgumentNullException(nameof(domainEvent));

            var @event = JsonConvert.DeserializeObject<ChangeBalanceCommand>(domainEvent.Json);

            return @event;
        }
    }
}