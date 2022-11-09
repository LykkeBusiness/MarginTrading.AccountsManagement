// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.AzureStorage
{
    internal class LossPercentageRepository: ILossPercentageRepository
    {
        public Task<ILossPercentage> GetLastAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task AddAsync(ILossPercentage value)
        {
            throw new System.NotImplementedException();
        }
    }
}