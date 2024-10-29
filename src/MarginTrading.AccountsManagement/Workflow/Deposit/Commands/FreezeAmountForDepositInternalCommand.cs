// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MessagePack;

namespace MarginTrading.AccountsManagement.Workflow.Deposit.Commands
{
    [MessagePackObject]
    public class FreezeAmountForDepositInternalCommand
    {
        [Key(0)]
        public string OperationId { get; }

        public FreezeAmountForDepositInternalCommand(string operationId)
        {
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
        }
    }
}