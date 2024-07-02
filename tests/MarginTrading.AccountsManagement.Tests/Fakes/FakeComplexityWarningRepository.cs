// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Repositories;

namespace MarginTrading.AccountsManagement.Tests.Fakes
{
    internal sealed class FakeComplexityWarningRepository : IComplexityWarningRepository
    {
        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public Task<ComplexityWarningState> GetOrCreate(string accountId, Func<ComplexityWarningState> factory)
        {
            return Task.FromResult(ComplexityWarningState.Start(accountId));
        }

        public Task Save(ComplexityWarningState entity)
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<ComplexityWarningState>> GetExpired(DateTime timestamp)
        {
            throw new NotImplementedException();
        }
    }
}