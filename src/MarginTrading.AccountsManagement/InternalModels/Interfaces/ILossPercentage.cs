// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.AccountsManagement.InternalModels.Interfaces
{
    public interface ILossPercentage
    {
        int Id { get; }

        int ClientNumber { get; }

        int LooserNumber { get; }

        DateTime Timestamp { get; }
    }
}