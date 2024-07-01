// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.MessageHandlers;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Orders;

using Microsoft.Extensions.Logging;

using Moq;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.Tests
{
    public class ProductComplexityWarningHandlerTests
    {
        private ProductComplexityWarningHandler _handler;
        private Mock<ILogger<ProductComplexityWarningHandler>> _loggerMock;
        private Mock<IAccountManagementService> _accountManagementServiceMock;
        private Mock<IComplexityWarningRepository> _complexityWarningRepositoryMock;
        private Mock<AccountManagementSettings> _settingsMock;
        private Mock<IComplexityWarningConfiguration> _complexityWarningConfigurationMock;
        private Mock<IOrderHistoryValidator> _orderHistoryValidatorMock;
        private Mock<IOrderValidator> _orderValidatorMock;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<ProductComplexityWarningHandler>>();
            _accountManagementServiceMock = new Mock<IAccountManagementService>();
            _complexityWarningRepositoryMock = new Mock<IComplexityWarningRepository>();
            _settingsMock = new Mock<AccountManagementSettings>();
            _complexityWarningConfigurationMock = new Mock<IComplexityWarningConfiguration>();
            _orderHistoryValidatorMock = new Mock<IOrderHistoryValidator>();
            _orderValidatorMock = new Mock<IOrderValidator>();

            _handler = new ProductComplexityWarningHandler(
                _loggerMock.Object,
                _accountManagementServiceMock.Object,
                _complexityWarningRepositoryMock.Object,
                _settingsMock.Object,
                _complexityWarningConfigurationMock.Object,
                _orderHistoryValidatorMock.Object,
                _orderValidatorMock.Object);
        }

        [Test]
        public async Task When_Feature_Is_Disabled_Should_Not_Handle_Message()
        {
            var message = new OrderHistoryEvent
            {
                OrderSnapshot = new OrderContract { AccountId = "accountId", Id = "orderId" }
            };
            var complexityWarningState = ComplexityWarningState.Start("accountId");
            _complexityWarningConfigurationMock.Setup(x => x.IsEnabled).ReturnsAsync(false);
            _orderHistoryValidatorMock
                .Setup(x => x.IsBasicAndPlaceTypeOrder(message))
                .Returns(true);
            _orderValidatorMock
                .Setup(x => x.ProductComplexityConfirmationReceived(It.IsAny<OrderContract>(), It.IsAny<bool>()))
                .Returns(true);
            _complexityWarningRepositoryMock
                .Setup(x => x.GetOrCreate(It.IsAny<string>(), It.IsAny<Func<ComplexityWarningState>>()))
                .ReturnsAsync(complexityWarningState);

            await _handler.Handle(message);

            CollectionAssert.DoesNotContain(complexityWarningState.ConfirmedOrders.Keys, "orderId");
        }

        [Test]
        public async Task When_Order_DoesNotMeet_Requirements_Handling_Should_Be_Skipped()
        {
            var message = new OrderHistoryEvent
            {
                OrderSnapshot = new OrderContract { AccountId = "accountId", Id = "orderId" }
            };
            var complexityWarningState = ComplexityWarningState.Start("accountId");
            _complexityWarningConfigurationMock.Setup(x => x.IsEnabled).ReturnsAsync(true);
            _orderHistoryValidatorMock
                .Setup(x => x.IsBasicAndPlaceTypeOrder(message))
                .Returns(false);
            _orderValidatorMock
                .Setup(x => x.ProductComplexityConfirmationReceived(It.IsAny<OrderContract>(), It.IsAny<bool>()))
                .Returns(true);
            _complexityWarningRepositoryMock
                .Setup(x => x.GetOrCreate(It.IsAny<string>(), It.IsAny<Func<ComplexityWarningState>>()))
                .ReturnsAsync(complexityWarningState);
            
            await _handler.Handle(message);
            
            CollectionAssert.DoesNotContain(complexityWarningState.ConfirmedOrders.Keys, "orderId");
        }

        [Test]
        public async Task Happy_Path()
        {
            var message = new OrderHistoryEvent
            {
                OrderSnapshot = new OrderContract
                {
                    AccountId = "accountId",
                    Id = "orderId"
                }
            };
            var complexityWarningState = ComplexityWarningState.Start("accountId");
            _complexityWarningConfigurationMock.Setup(x => x.IsEnabled).ReturnsAsync(true);
            _orderHistoryValidatorMock
                .Setup(x => x.IsBasicAndPlaceTypeOrder(message))
                .Returns(true);
            _orderValidatorMock
                .Setup(x => x.ProductComplexityConfirmationReceived(It.IsAny<OrderContract>(), It.IsAny<bool>()))
                .Returns(true);
            _complexityWarningRepositoryMock
                .Setup(x => x.GetOrCreate(It.IsAny<string>(), It.IsAny<Func<ComplexityWarningState>>()))
                .ReturnsAsync(complexityWarningState);
            
            CollectionAssert.DoesNotContain(complexityWarningState.ConfirmedOrders.Keys, "orderId");
            
            await _handler.Handle(message);
            
            CollectionAssert.Contains(complexityWarningState.ConfirmedOrders.Keys, "orderId");
        }
    }
}