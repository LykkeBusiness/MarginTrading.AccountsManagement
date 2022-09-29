// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.AccountsManagement.Infrastructure.Implementation;
using NUnit.Framework;

namespace MarginTrading.AccountsManagement.Tests
{
    public class MappingTests
    {
        [Test]
        public void ShouldHaveValidMappingConfiguration()
        {
            var convertService = new ConvertService();
            convertService.AssertConfigurationIsValid();
        }
    }
}