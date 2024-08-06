// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using MarginTrading.AccountsManagement.Services;

namespace MarginTrading.AccountsManagement.Tests.Fakes
{
    internal sealed class EnabledComplexityWarningConfiguration : IComplexityWarningConfiguration
    {
        public Task<bool> IsEnabled => Task.FromResult(true);
        public Task Validate()
        {
            throw new NotImplementedException();
        }
    }
}