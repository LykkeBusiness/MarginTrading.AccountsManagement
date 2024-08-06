// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using Lykke.RabbitMqBroker.Publisher;

using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.MessageHandlers;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Settings;

using NUnit.Framework;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

using Moq;

namespace MarginTrading.AccountsManagement.Tests
{
    public class EodFinishedHandlerTests
    {
        private Mock<ILossPercentageRepository> _lossPercentageRepositoryMock;
        private Mock<IAccountHistoryRepository> _accountHistoryRepositoryMock;
        private Mock<ISystemClock> _systemClockMock;
        private Mock<ILogger<EodFinishedHandler>> _loggerMock;
        private Mock<IRabbitPublisher<AutoComputedLossPercentageUpdateEvent>> _lossPercentageProducerMock;
        private const string BrokerId = "TestBrokerId";
        
        [SetUp]
        public void SetUp()
        {
            _lossPercentageRepositoryMock = new Mock<ILossPercentageRepository>();
            _accountHistoryRepositoryMock = new Mock<IAccountHistoryRepository>();
            _systemClockMock = new Mock<ISystemClock>();
            _loggerMock = new Mock<ILogger<EodFinishedHandler>>();
            _lossPercentageProducerMock = new Mock<IRabbitPublisher<AutoComputedLossPercentageUpdateEvent>>();
        }
        
        [Test]
        public async Task CalculateLossPercentageIfNeeded_LastTimestampNotExprired_Skips()
        {
            //arrange
            var utcNow = DateTime.UtcNow;
            var lossPercentageMoc = new Mock<ILossPercentage>();
            lossPercentageMoc.Setup(x => x.Timestamp).Returns(utcNow.AddDays(-1));
            _lossPercentageRepositoryMock.Setup(x => x.GetLastAsync())
                .ReturnsAsync(lossPercentageMoc.Object);
            _systemClockMock.Setup(x => x.UtcNow).Returns(utcNow);
            var listener = new EodFinishedHandler(
                _lossPercentageRepositoryMock.Object,
                _accountHistoryRepositoryMock.Object,
                _systemClockMock.Object,
                new AccountManagementSettings
                {
                    LossPercentageExpirationCheckPeriodInDays = 2
                },
                _lossPercentageProducerMock.Object,
                BrokerId,
                _loggerMock.Object);

            await listener.Handle(null);

            //assert
            _systemClockMock.Verify(mock => mock.UtcNow, Times.Once);
            _lossPercentageRepositoryMock.Verify(x => x.GetLastAsync(), Times.Once);
            _accountHistoryRepositoryMock.Verify(mock => mock.CalculateLossPercentageAsync(It.IsAny<DateTime>()), Times.Never);
        }
        
        [Test]
        public async Task CalculateLossPercentageIfNeeded_LastLossPercentageIsNull_Calculates()
        {
            //arrange
            var utcNow = DateTime.UtcNow;
            var lossPercentageCalculationPeriodInDays = 1;
            var lossPercentageCalculationPeriod = new TimeSpan(lossPercentageCalculationPeriodInDays, 0, 0, 0);
            _lossPercentageRepositoryMock.Setup(x => x.GetLastAsync())
                .ReturnsAsync((ILossPercentage)null);
            _systemClockMock.Setup(x => x.UtcNow).Returns(utcNow);
            var accountHistoryLossPercentageMock = new Mock<IAccountHistoryLossPercentage>();
            accountHistoryLossPercentageMock.Setup(x => x.ClientNumber).Returns(100);
            accountHistoryLossPercentageMock.Setup(x => x.LooserNumber).Returns(50);
            _accountHistoryRepositoryMock
                .Setup(x => x.CalculateLossPercentageAsync(utcNow.Subtract(lossPercentageCalculationPeriod)))
                .ReturnsAsync(accountHistoryLossPercentageMock.Object);
            var listener = new EodFinishedHandler(
                _lossPercentageRepositoryMock.Object,
                _accountHistoryRepositoryMock.Object,
                _systemClockMock.Object,
                new AccountManagementSettings
                {
                    LossPercentageCalculationPeriodInDays = lossPercentageCalculationPeriodInDays
                },
                _lossPercentageProducerMock.Object,
                BrokerId,
                _loggerMock.Object);

            await listener.Handle(null);

            _lossPercentageRepositoryMock.Verify(x => x.AddAsync(
                It.Is<LossPercentage>(arg => arg.ClientNumber == 100
                                             && arg.LooserNumber == 50
                                             && arg.Timestamp == utcNow)),
                Times.Once);
            _lossPercentageProducerMock.Verify(x =>x.PublishAsync(
                    It.Is<AutoComputedLossPercentageUpdateEvent>(arg => arg.BrokerId == BrokerId
                                                                        && arg.Timestamp == utcNow
                                                                        && arg.Value == 50M)),
                Times.Once);
        }
        
        [TestCase(100, 50, 50)]
        [TestCase(0, 0, 0)]
        [TestCase(100000, 12, 0.01)]
        [TestCase(100000, 15, 0.02)]
        [TestCase(100000, 16, 0.02)]
        [TestCase(100000, 22, 0.02)]
        [TestCase(100000, 25, 0.02)]
        [TestCase(100000, 26, 0.03)]
        public async Task CalculateLossPercentageIfNeeded_LastLossPercentageNotNullAndExpired_CalculatesAndRoundsIfNeeded(
            int clientNumber,
            int looserNumber,
            decimal lossPercentageValue)
        {
            //arrange
            var utcNow = DateTime.UtcNow;
            var lossPercentageCalculationPeriodInDays = 1;
            var lossPercentageCalculationPeriod = new TimeSpan(lossPercentageCalculationPeriodInDays, 0, 0, 0);
            var lossPercentageMock = new Mock<ILossPercentage>();
            lossPercentageMock.Setup(x => x.Timestamp).Returns(utcNow.AddDays(-2));
            _lossPercentageRepositoryMock.Setup(x => x.GetLastAsync())
                .ReturnsAsync(lossPercentageMock.Object);
            _systemClockMock.Setup(x => x.UtcNow).Returns(utcNow);
            var accountHistoryLossPercentageMock = new Mock<IAccountHistoryLossPercentage>();
            accountHistoryLossPercentageMock.Setup(x => x.ClientNumber).Returns(clientNumber);
            accountHistoryLossPercentageMock.Setup(x => x.LooserNumber).Returns(looserNumber);
            _accountHistoryRepositoryMock
                .Setup(x => x.CalculateLossPercentageAsync(utcNow.Subtract(lossPercentageCalculationPeriod)))
                .ReturnsAsync(accountHistoryLossPercentageMock.Object);
            var listener = new EodFinishedHandler(
                _lossPercentageRepositoryMock.Object,
                _accountHistoryRepositoryMock.Object,
                _systemClockMock.Object,
                new AccountManagementSettings
                {
                    LossPercentageExpirationCheckPeriodInDays = 1,
                    LossPercentageCalculationPeriodInDays = lossPercentageCalculationPeriodInDays
                },
                _lossPercentageProducerMock.Object,
                BrokerId,
                _loggerMock.Object);

            await listener.Handle(null);

            //assert
            _lossPercentageRepositoryMock.Verify(x => x.AddAsync(
                It.Is<LossPercentage>(arg => arg.ClientNumber == clientNumber
                                             && arg.LooserNumber == looserNumber
                                             && arg.Timestamp == utcNow)),
                Times.Once);
            _lossPercentageProducerMock.Verify(x =>x.PublishAsync(
                    It.Is<AutoComputedLossPercentageUpdateEvent>(arg => arg.BrokerId == BrokerId
                                                                        && arg.Timestamp == utcNow
                                                                        && arg.Value == lossPercentageValue)),
                Times.Once);
        }
    }
}