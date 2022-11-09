// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;

namespace MarginTrading.AccountsManagement.InternalModels
{
    public class LossPercentage: ILossPercentage
    {
        public LossPercentage(
            int clientNumber,
            int looserNumber,
            DateTime timestamp)
        {
            ClientNumber = clientNumber;
            LooserNumber = looserNumber;
            Timestamp = timestamp;
        }

        public int Id { get; }
        public int ClientNumber { get; }
        public int LooserNumber { get; }
        public DateTime Timestamp { get; }
    }
}