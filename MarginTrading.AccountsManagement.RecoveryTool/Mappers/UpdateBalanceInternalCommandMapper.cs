// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.RecoveryTool.Model;
using MarginTrading.AccountsManagement.Extensions;

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

        public UpdateBalanceInternalCommand Map(ChangeBalanceCommand command)
        {
            var @event = new UpdateBalanceInternalCommand(
                command.OperationId,
                command.AccountId,
                command.Amount,
                command.Reason,
                command.AuditLog,
                $"{command.ReasonType.ToString()} command",
                command.ReasonType.ToType<AccountBalanceChangeReasonType>(),
                command.EventSourceId,
                command.AssetPairId,
                command.TradingDay
            );

            return @event;
        }
    }
}