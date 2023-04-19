// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AccountsManagement.Workflow.UpdateBalance.Commands;
using MarginTrading.Backend.Contracts.Events;

namespace MarginTrading.AccountsManagement.Workflow.ClosePosition
{
    internal class ClosePositionSaga
    {
        private readonly CqrsContextNamesSettings _contextNames;

        public ClosePositionSaga(CqrsContextNamesSettings contextNames)
        {
            _contextNames = contextNames;
        }

        /// <summary>
        /// The position is closed => update the balance
        /// </summary>
        [UsedImplicitly]
        private void Handle(PositionClosedEvent evt, ICommandSender sender)
        {
            var operationId = new OperationId(evt.PositionId).ExtendWithUpdateBalance();

            sender.SendCommand(
                new UpdateBalanceInternalCommand(
                    operationId,
                    evt.AccountId,
                    evt.BalanceDelta,
                    $"Balance changed on position close (id = {evt.PositionId})",
                    string.Empty,
                    nameof(ClosePositionSaga),
                    AccountBalanceChangeReasonType.RealizedPnL,
                    evt.PositionId,
                    evt.AssetPairId,
                    DateTime.UtcNow),
                _contextNames.AccountsManagement);
        }
    }
}