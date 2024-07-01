// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using Lykke.Snow.Common.Startup;
using Lykke.Snow.Mdm.Contracts.Models.Contracts;
using Lykke.Snow.Mdm.Contracts.Models.Events;

using MarginTrading.AccountsManagement.MessageHandlers;
using MarginTrading.AccountsManagement.Services;

using Moq;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.Tests
{
    public class BrokerSettingsHandlerTests
    {
        private Mock<IBrokerSettingsCache> _brokerSettingsCacheMock;
        private BrokerId _brokerId;
        private BrokerSettingsHandler _handler;
        
        [SetUp]
        public void SetUp()
        {
            _brokerSettingsCacheMock = new Mock<IBrokerSettingsCache>();
            _brokerId = new BrokerId("1");
            _handler = new BrokerSettingsHandler(_brokerSettingsCacheMock.Object, _brokerId);
        }

        [Test]
        public async Task Handle_ShouldUpdateCache_WhenChangeTypeIsEditionAndBrokerIdMatches()
        {
            var message = new BrokerSettingsChangedEvent
            {
                ChangeType = ChangeType.Edition,
                NewValue = new BrokerSettingsContract { BrokerId = _brokerId }
            };

            await _handler.Handle(message);

            _brokerSettingsCacheMock.Verify(cache => cache.Update(message.NewValue), Times.Once);
        }
        
        [TestCase(ChangeType.Creation)]
        [TestCase(ChangeType.Deletion)]
        public async Task Handle_ShouldNotUpdateCache_WhenChangeTypeIsNotEdition(ChangeType changeType)
        {
            var message = new BrokerSettingsChangedEvent
            {
                ChangeType = changeType,
                NewValue = new BrokerSettingsContract { BrokerId = _brokerId }
            };

            await _handler.Handle(message);

            _brokerSettingsCacheMock.Verify(cache => cache.Update(It.IsAny<BrokerSettingsContract>()), Times.Never);
        }
        
        [Test]
        public async Task Handle_ShouldNotUpdateCache_WhenBrokerIdDoesNotMatch()
        {
            var message = new BrokerSettingsChangedEvent
            {
                ChangeType = ChangeType.Edition,
                NewValue = new BrokerSettingsContract { BrokerId = new BrokerId("2") }
            };

            await _handler.Handle(message);

            _brokerSettingsCacheMock.Verify(cache => cache.Update(It.IsAny<BrokerSettingsContract>()), Times.Never);
        }
    }
}