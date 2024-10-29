// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MessagePack;

namespace MarginTrading.AccountsManagement.Workflow.Deposit.Commands
{
    [MessagePackObject]
    public class CompleteDepositInternalCommand
    {
        [Key(0)]
        public string OperationId { get; }

       
        public CompleteDepositInternalCommand([NotNull] string operationId)
        {
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
        }
    }
}