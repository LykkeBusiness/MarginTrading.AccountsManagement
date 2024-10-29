// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MessagePack;

namespace MarginTrading.AccountsManagement.Workflow.Withdrawal.Commands
{
    [MessagePackObject]
    public class FailWithdrawalInternalCommand
    {
        [Key(0)]
        public string OperationId { get; }
        
        [Key(1)]
        public string Reason { get; }
       

        public FailWithdrawalInternalCommand([NotNull] string operationId, string reason)
        {
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
            Reason = reason;
        }
    }
}