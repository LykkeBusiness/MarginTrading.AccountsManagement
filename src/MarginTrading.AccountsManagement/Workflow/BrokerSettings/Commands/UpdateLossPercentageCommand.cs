// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MessagePack;

namespace MarginTrading.AccountsManagement.Workflow.BrokerSettings.Commands
{
    [MessagePackObject]
    public class UpdateLossPercentageCommand
    {
        [Key(0)] public decimal Value { get; set; }

        [Key(1)] public DateTime Timestamp { get; set; }
    }
}