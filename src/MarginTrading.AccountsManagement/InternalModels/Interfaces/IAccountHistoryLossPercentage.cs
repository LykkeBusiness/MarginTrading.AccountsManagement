// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.AccountsManagement.InternalModels.Interfaces
{
    public interface IAccountHistoryLossPercentage
    {
        int ClientNumber { get; }

        int LooserNumber { get; }
    }
}