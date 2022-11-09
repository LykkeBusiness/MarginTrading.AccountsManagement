// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using MessagePack;

namespace MarginTrading.AccountsManagement.Contracts.Commands
{
    [MessagePackObject]
    public class UpdateLossPercentageCommand
    {
        [Key(0)] public string BrokerId { get; set; }
        
        [Key(1)] public decimal Value { get; set; }

        [Key(2)] public DateTime Timestamp { get; set; }
    }
}