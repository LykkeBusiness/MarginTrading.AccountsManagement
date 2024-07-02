using System;
using System.Threading.Tasks;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.IntegrationTests.WorkflowTests
{
    public class CloseOrderTests
    {
        [TestCase(-10000)]
        [TestCase(10000)]
        public async Task Always_ShouldUpdateBalance(decimal delta)
        {
            // arrange
            await TestsHelpers.EnsureAccountState();
            var operationId = Guid.NewGuid().ToString();
/*
            // act
            //todo use specific command
            /*CqrsUtil.SendCommandToAccountManagement(
                new BeginClosePositionBalanceUpdateCommand(TestsHelpers.ClientId, TestsHelpers.AccountId, delta,
                    operationId, "IntegrationalTests", "Always_ShouldUpdateBalance"));

            await RabbitUtil.WaitForCqrsMessage<AccountBalanceChangedEvent>(m => m.OperationId == operationId);

            // assert
            (await TestsHelpers.GetAccount()).Balance.Should().Be(0 + delta);
            */
        }
    }
}