// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using JetBrains.Annotations;

using MarginTrading.AccountsManagement.InternalModels.Interfaces;

namespace MarginTrading.AccountsManagement.Repositories
{
    public interface ILossPercentageRepository
    {
        [ItemCanBeNull]
        Task<ILossPercentage> GetLastAsync();
        
        Task AddAsync(ILossPercentage value);
    }
}