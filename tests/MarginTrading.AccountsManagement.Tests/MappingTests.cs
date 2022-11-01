// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.AccountsManagement.Infrastructure.Implementation;
using NUnit.Framework;
using BrokerConvertService = MarginTrading.AccountsManagement.AccountHistoryBroker.Services.ConvertService;

namespace MarginTrading.AccountsManagement.Tests
{
    public class MappingTests
    {
        [Test]
        public void Host_ShouldHaveValidMappingConfiguration()
        {
            var convertService = new ConvertService();
            convertService.AssertConfigurationIsValid();
        }

        [Test]
        public void Broker_ShouldHaveValidMappingConfiguration()
        {
            var convertService = new BrokerConvertService();
            convertService.AssertConfigurationIsValid();
        }
    }
}