// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using AutoMapper;

using MarginTrading.AccountsManagement.InternalModels.Interfaces;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    public class LossPercentageEntity: ILossPercentage
    {
        [IgnoreMap]
        public int Id { get; set; }
        public int ClientNumber { get; set; }
        public int LooserNumber { get; set; }
        public DateTime Timestamp { get; set; }
    }
}