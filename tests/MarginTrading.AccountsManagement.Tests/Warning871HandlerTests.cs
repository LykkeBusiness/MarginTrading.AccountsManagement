// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using MarginTrading.AccountsManagement.MessageHandlers;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Orders;

using Microsoft.Extensions.Logging;

using Moq;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.Tests
{
    public class Warning871HandlerTests
    {
        private Mock<IAccountManagementService> _accountManagementServiceMock;
        private Mock<ILogger<Warning871Handler>> _loggerMock;
        private Mock<IOrderHistoryValidator> _orderHistoryValidatorMock;
        private Mock<IOrderValidator> _orderValidatorMock;
        private Warning871Handler _handler;

        [SetUp]
        public void SetUp()
        {
            _accountManagementServiceMock = new Mock<IAccountManagementService>();
            _loggerMock = new Mock<ILogger<Warning871Handler>>();
            _orderHistoryValidatorMock = new Mock<IOrderHistoryValidator>();
            _orderValidatorMock = new Mock<IOrderValidator>();
            _handler = new Warning871Handler(
                _loggerMock.Object,
                _accountManagementServiceMock.Object,
                _orderHistoryValidatorMock.Object,
                _orderValidatorMock.Object);
        }

        [Test]
        public async Task When_Order_DoesNotMeet_Requirements_Handling_Should_Be_Skipped()
        {
            var messageMock = new Mock<OrderHistoryEvent>();
            _orderHistoryValidatorMock
                .Setup(x => x.IsBasicAndPlaceTypeOrder(messageMock.Object))
                .Returns(false);

            await _handler.Handle(messageMock.Object);

            _accountManagementServiceMock.Verify(
                service => service.Update871mWarningFlag(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>()),
                Times.Never);
        }
        
        [Test]
        public async Task When_Order_Meets_Requirements_And_Warning871_Confirmed_Handling_Should_Be_Performed()
        {
            var message = new OrderHistoryEvent
            {
                OrderSnapshot = new OrderContract
                {
                    AccountId = "accountId",
                    Id = "orderId"
                }
            };
            _orderHistoryValidatorMock
                .Setup(x => x.IsBasicAndPlaceTypeOrder(message))
                .Returns(true);
            _orderValidatorMock
                .Setup(x => x.Warning871mConfirmed(message.OrderSnapshot))
                .Returns(true);

            await _handler.Handle(message);

            _accountManagementServiceMock.Verify(
                service => service.Update871mWarningFlag(
                    message.OrderSnapshot.AccountId,
                    It.IsAny<bool>(),
                    message.OrderSnapshot.Id),
                Times.Once);
        }
        
        [Test]
        public async Task When_Order_Warning_Not_871Confirmed_Handling_Should_Be_Skipped()
        {
            var message = new OrderHistoryEvent
            {
                OrderSnapshot = new OrderContract
                {
                    AccountId = "accountId",
                    Id = "orderId"
                }
            };
            _orderHistoryValidatorMock
                .Setup(x => x.IsBasicAndPlaceTypeOrder(message))
                .Returns(true);
            _orderValidatorMock
                .Setup(x => x.Warning871mConfirmed(message.OrderSnapshot))
                .Returns(false);

            await _handler.Handle(message);

            _accountManagementServiceMock.Verify(
                service => service.Update871mWarningFlag(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>()),
                Times.Never);
        }
    }
}