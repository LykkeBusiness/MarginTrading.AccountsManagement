using System;
using JetBrains.Annotations;
using MessagePack;

namespace MarginTrading.AccountsManagement.Workflow.Deposit.Commands
{
    [MessagePackObject]
    public class FailDepositInternalCommand
    {
        [Key(0)]
        public string OperationId { get; }

        public FailDepositInternalCommand([NotNull] string operationId)
        {
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
        }
    }
}