// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MessagePack;

namespace MarginTrading.AccountsManagement.Workflow.Withdrawal.Commands
{
    [MessagePackObject]
    public class CompleteWithdrawalInternalCommand
    {
        [Key(0)]
        public string OperationId { get; }

        public CompleteWithdrawalInternalCommand([NotNull] string operationId)
        {
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
        }
    }
}