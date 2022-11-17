// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

using BookKeeper.Client.Workflow.Events;

using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;

using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AccountsManagement.Workflow.BrokerSettings;

using NUnit.Framework;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

using Moq;

namespace MarginTrading.AccountsManagement.Tests
{
    public class EodProcessFinishedListenerTests
    {
        private Mock<ILossPercentageRepository> _lossPercentageRepositoryMock;
        private Mock<IAccountHistoryRepository> _accountHistoryRepositoryMock;
        private Mock<ISystemClock> _systemClockMock;
        private Mock<ILogger<EodProcessFinishedListener>> _loggerMock;
        private Mock<IRabbitPublisher<AutoComputedLossPercentageUpdateEvent>> _lossPercentageProducerMock;
        private const string BrokerId = "TestBrokerId";
        
        [SetUp]
        public void SetUp()
        {
            _lossPercentageRepositoryMock = new Mock<ILossPercentageRepository>();
            _accountHistoryRepositoryMock = new Mock<IAccountHistoryRepository>();
            _systemClockMock = new Mock<ISystemClock>();
            _loggerMock = new Mock<ILogger<EodProcessFinishedListener>>();
            _lossPercentageProducerMock = new Mock<IRabbitPublisher<AutoComputedLossPercentageUpdateEvent>>();
        }
        
        [Test]
        public void CalculateLossPercentageIfNeeded_LastTimestampNotExprired_Skips()
        {
            //arrange
            var utcNow = DateTime.UtcNow;
            var lossPercentageMoc = new Mock<ILossPercentage>();
            lossPercentageMoc.Setup(x => x.Timestamp).Returns(utcNow.AddDays(-1));
            _lossPercentageRepositoryMock.Setup(x => x.GetLastAsync())
                .ReturnsAsync(lossPercentageMoc.Object);
            _systemClockMock.Setup(x => x.UtcNow).Returns(utcNow);
            var listener = new EodProcessFinishedListener(
                _lossPercentageRepositoryMock.Object,
                _accountHistoryRepositoryMock.Object,
                _systemClockMock.Object,
                _loggerMock.Object,
                It.IsAny<RabbitMqSubscriber<EodProcessFinishedEvent>>(),
                new AccountManagementSettings
                {
                    LossPercentageExpirationCheckPeriod = new TimeSpan(2,0,0,0)
                },
                _lossPercentageProducerMock.Object,
                BrokerId);

            //act
            typeof(EodProcessFinishedListener).GetMethod("CalculateLossPercentageIfNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(listener, null);

            //assert
            _systemClockMock.Verify(mock => mock.UtcNow, Times.Once);
            _lossPercentageRepositoryMock.Verify(x => x.GetLastAsync(), Times.Once);
            _accountHistoryRepositoryMock.Verify(mock => mock.CalculateLossPercentageAsync(It.IsAny<DateTime>()), Times.Never);
        }
        
        [Test]
        public void CalculateLossPercentageIfNeeded_LastLossPercentageIsNull_Calculates()
        {
            //arrange
            var utcNow = DateTime.UtcNow;
            var lossPercentageCalculationPeriod = new TimeSpan(1, 0, 0, 0);
            _lossPercentageRepositoryMock.Setup(x => x.GetLastAsync())
                .ReturnsAsync((ILossPercentage)null);
            _systemClockMock.Setup(x => x.UtcNow).Returns(utcNow);
            var accountHistoryLossPercentageMock = new Mock<IAccountHistoryLossPercentage>();
            accountHistoryLossPercentageMock.Setup(x => x.ClientNumber).Returns(100);
            accountHistoryLossPercentageMock.Setup(x => x.LooserNumber).Returns(50);
            _accountHistoryRepositoryMock
                .Setup(x => x.CalculateLossPercentageAsync(utcNow.Subtract(lossPercentageCalculationPeriod)))
                .ReturnsAsync(accountHistoryLossPercentageMock.Object);
            var listener = new EodProcessFinishedListener(
                _lossPercentageRepositoryMock.Object,
                _accountHistoryRepositoryMock.Object,
                _systemClockMock.Object,
                _loggerMock.Object,
                It.IsAny<RabbitMqSubscriber<EodProcessFinishedEvent>>(),
                new AccountManagementSettings
                {
                    LossPercentageCalculationPeriod = lossPercentageCalculationPeriod
                },
                _lossPercentageProducerMock.Object,
                BrokerId);

            //act
            typeof(EodProcessFinishedListener).GetMethod("CalculateLossPercentageIfNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(listener, null);

            //assert
            _lossPercentageRepositoryMock.Verify(x => x.AddAsync(
                It.Is<LossPercentage>(arg => arg.ClientNumber == 100
                                             && arg.LooserNumber == 50
                                             && arg.Timestamp == utcNow)),
                Times.Once);
            _lossPercentageProducerMock.Verify(x =>x.PublishAsync(
                    It.Is<AutoComputedLossPercentageUpdateEvent>(arg => arg.BrokerId == BrokerId
                                                                        && arg.Timestamp == utcNow
                                                                        && arg.Value == 0.5M)),
                Times.Once);
        }
        
        [TestCase(100, 50, 0.5)]
        [TestCase(0, 0, 0)]
        public void CalculateLossPercentageIfNeeded_LastLossPercentageNotNullAndExpired_Calculates(
            int clientNumber,
            int looserNumber,
            decimal lossPercentageValue)
        {
            //arrange
            var utcNow = DateTime.UtcNow;
            var lossPercentageCalculationPeriod = new TimeSpan(1, 0, 0, 0);
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
            var listener = new EodProcessFinishedListener(
                _lossPercentageRepositoryMock.Object,
                _accountHistoryRepositoryMock.Object,
                _systemClockMock.Object,
                _loggerMock.Object,
                It.IsAny<RabbitMqSubscriber<EodProcessFinishedEvent>>(),
                new AccountManagementSettings
                {
                    LossPercentageExpirationCheckPeriod = new TimeSpan(1,0,0,0),
                    LossPercentageCalculationPeriod = lossPercentageCalculationPeriod
                },
                _lossPercentageProducerMock.Object,
                BrokerId);

            //act
            typeof(EodProcessFinishedListener).GetMethod("CalculateLossPercentageIfNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(listener, null);

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