// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using MarginTrading.AccountsManagement.MessageHandlers;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AccountsManagement.Tests.Fakes;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Orders;

using Microsoft.Extensions.Logging;

using Moq;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.Tests
{
    public class ProductComplexityWarningHandlerTests
    {
        private Mock<ILogger<ProductComplexityWarningHandler>> _loggerMock;
        private ComplexityWarningFlagsAccountManagementService _accountManagementService;
        private AccountManagementSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<ProductComplexityWarningHandler>>();
            _accountManagementService = new ComplexityWarningFlagsAccountManagementService();
            _settings = new AccountManagementSettings { ComplexityWarningsCount = 1 };
        }

        [Test]
        public async Task When_Feature_Is_Disabled_Should_Not_Handle_Message()
        {
            var message = CreateOrderHistoryEvent();

            var sut = new ProductComplexityWarningHandler(
                _loggerMock.Object,
                _accountManagementService,
                new FakeComplexityWarningRepository(),
                _settings,
                new DisabledComplexityWarningConfiguration(),
                new PositiveOrderHistoryValidator(),
                new PositiveOrderValidator());
                
            await sut.Handle(message);
            
            CollectionAssert.DoesNotContain(_accountManagementService.ProductComplexityWarningFlags.Keys, "accountId");
        }

        [Test]
        public async Task When_Order_DoesNotMeet_Requirements_Handling_Should_Be_Skipped()
        {
            var message = CreateOrderHistoryEvent();
            
            var sut = new ProductComplexityWarningHandler(
                _loggerMock.Object,
                _accountManagementService,
                new FakeComplexityWarningRepository(),
                _settings,
                new EnabledComplexityWarningConfiguration(),
                new NegativeOrderHistoryValidator(),
                new PositiveOrderValidator());

            await sut.Handle(message);
            
            CollectionAssert.DoesNotContain(_accountManagementService.ProductComplexityWarningFlags.Keys, "accountId");
        }

        [Test]
        public async Task Happy_Path()
        {
            var message = CreateOrderHistoryEvent();
            
            var sut = new ProductComplexityWarningHandler(
                _loggerMock.Object,
                _accountManagementService,
                new FakeComplexityWarningRepository(),
                _settings,
                new EnabledComplexityWarningConfiguration(),
                new PositiveOrderHistoryValidator(),
                new PositiveOrderValidator());
            
            await sut.Handle(message);
            
            CollectionAssert.Contains(_accountManagementService.ProductComplexityWarningFlags.Keys, "accountId");
        }
        
        private static OrderHistoryEvent CreateOrderHistoryEvent(string accountId = "accountId", string orderId = "orderId")
        {
            return new OrderHistoryEvent
            {
                OrderSnapshot = new OrderContract { AccountId = accountId, Id = orderId }
            };
        }
    }
}