// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using FluentAssertions;

using Lykke.Snow.Common.Startup;
using Lykke.Snow.Mdm.Contracts.Models.Contracts;
using Lykke.Snow.Mdm.Contracts.Models.Events;

using MarginTrading.AccountsManagement.MessageHandlers;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Tests.Fakes;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.Tests
{
    public class BrokerSettingsHandlerTests
    {
        private IBrokerSettingsCache _brokerSettingsCache;
        private BrokerId _brokerId;
        private BrokerSettingsHandler _handler;
        
        [SetUp]
        public void SetUp()
        {
            _brokerSettingsCache = new SomeBrokerSettingsCache();
            _brokerSettingsCache.Initialize();

            _brokerId = _brokerSettingsCache.Get().BrokerId;
            _handler = new BrokerSettingsHandler(_brokerSettingsCache, _brokerId);
        }

        [Test]
        public async Task Handle_ShouldUpdateCache_WhenChangeTypeIsEditionAndBrokerIdMatches()
        {
            var message = new BrokerSettingsChangedEvent
            {
                ChangeType = ChangeType.Edition,
                NewValue = new BrokerSettingsContract { BrokerId = _brokerId, Timezone = "Test1"}
            };

            await _handler.Handle(message);
            
            _brokerSettingsCache.Get().Timezone.Should().Be("Test1");
        }
        
        [TestCase(ChangeType.Creation)]
        [TestCase(ChangeType.Deletion)]
        public async Task Handle_ShouldNotUpdateCache_WhenChangeTypeIsNotEdition(ChangeType changeType)
        {
            var message = new BrokerSettingsChangedEvent
            {
                ChangeType = changeType,
                NewValue = new BrokerSettingsContract { BrokerId = _brokerId, Timezone = "Test2" }
            };
        
            await _handler.Handle(message);
        
            _brokerSettingsCache.Get().Timezone.Should().NotBe("Test2");
        }
        
        [Test]
        public async Task Handle_ShouldNotUpdateCache_WhenBrokerIdDoesNotMatch()
        {
            var message = new BrokerSettingsChangedEvent
            {
                ChangeType = ChangeType.Edition,
                NewValue = new BrokerSettingsContract { BrokerId = new BrokerId("2"), Timezone = "Test3" }
            };
        
            await _handler.Handle(message);
            
            _brokerSettingsCache.Get().Timezone.Should().NotBe("Test3");
        }
    }
}