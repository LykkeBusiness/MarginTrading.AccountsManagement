// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;

namespace MarginTrading.AccountsManagement.Repositories
{
    public interface IAccountHistoryRepository
    {
        Task<IAccountHistoryLossPercentage> CalculateLossPercentageAsync(DateTime from);
    }
}