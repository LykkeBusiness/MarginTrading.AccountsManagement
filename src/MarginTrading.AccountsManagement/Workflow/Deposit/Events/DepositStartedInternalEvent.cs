// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.AccountsManagement.Contracts.Events;
using MessagePack;

namespace MarginTrading.AccountsManagement.Workflow.Deposit.Events
{
    [MessagePackObject]
    public class DepositStartedInternalEvent : BaseEvent
    {
      

        public DepositStartedInternalEvent(string operationId, DateTime eventTimestamp)
            : base(operationId, eventTimestamp)
        {
           
        }
    }
}