// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MessagePack;

namespace MarginTrading.AccountsManagement.InternalModels
{
    [MessagePackObject]
    public class TemporaryCapital
    {
        [Key(0)]
        public string Id { get; set; }
        
        [Key(1)]
        public decimal Amount { get; set; }
    }
}