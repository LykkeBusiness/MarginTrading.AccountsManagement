// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using FluentAssertions;

using MarginTrading.AccountsManagement.MessageHandlers;
using MarginTrading.AccountsManagement.Tests.Fakes;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Orders;

using Microsoft.Extensions.Logging;

using Moq;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.Tests
{
    public class Warning871HandlerTests
    {
        private Warning871mFlagsAccountManagementService _accountManagementService;
        private Mock<ILogger<Warning871Handler>> _loggerMock;

        [SetUp]
        public void SetUp()
        {
            _accountManagementService = new Warning871mFlagsAccountManagementService();
            _loggerMock = new Mock<ILogger<Warning871Handler>>();
        }

        [Test]
        public async Task When_Order_DoesNotMeet_Requirements_Handling_Should_Be_Skipped()
        {
            var sut = new Warning871Handler(
                _loggerMock.Object,
                _accountManagementService,
                new NegativeOrderHistoryValidator(),
                new NegativeOrderValidator());

            await sut.Handle(new OrderHistoryEvent { OrderSnapshot = new OrderContract { AccountId = "accountId" } });

            _accountManagementService.WarningFlags871m.Should().BeEmpty();
        }

        [Test]
        public async Task When_Order_Meets_Requirements_And_Warning871_Confirmed_Handling_Should_Be_Performed()
        {
            var sut = new Warning871Handler(
                _loggerMock.Object,
                _accountManagementService,
                new PositiveOrderHistoryValidator(),
                new PositiveOrderValidator());
        
            var message = new OrderHistoryEvent
            {
                OrderSnapshot = new OrderContract
                {
                    AccountId = "accountId",
                    Id = "orderId"
                }
            };
            await sut.Handle(message);
        
            _accountManagementService.WarningFlags871m.Should().ContainKey("accountId");
        }
        
        [Test]
        public async Task When_Order_Warning_Not_871Confirmed_Handling_Should_Be_Skipped()
        {
            var sut = new Warning871Handler(
                _loggerMock.Object,
                _accountManagementService,
                new PositiveOrderHistoryValidator(),
                new NegativeOrderValidator());
            
            var message = new OrderHistoryEvent
            {
                OrderSnapshot = new OrderContract
                {
                    AccountId = "accountId",
                    Id = "orderId"
                }
            };
        
            await sut.Handle(message);
            
            _accountManagementService.WarningFlags871m.Should().BeEmpty();
        }
    }
}