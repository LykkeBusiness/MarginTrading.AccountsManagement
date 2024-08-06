using System;
using System.Threading.Tasks;

using FluentAssertions;

using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.IntegrationTests.Infrastructure;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.IntegrationTests.WorkflowTests
{
    public class DepositTests
    {
        [Test]
        public async Task Always_ShouldDeposit()
        {
            // arrange
            await TestsHelpers.EnsureAccountState();

            // act
            var balanceBefore = (await TestsHelpers.GetAccount()).Balance;
            var operationId = await ClientUtil.AccountsApi.BeginDeposit(
                TestsHelpers.AccountId,
                new AccountChargeManuallyRequest
                {
                    OperationId = Guid.NewGuid().ToString("N"),
                    AmountDelta = 123,
                    Reason = "intergational tests: deposit",
                });
            
            var messagesReceivedTask = Task.WhenAll(
                RabbitUtil.WaitForCqrsMessage<AccountChangedEvent>(m => m.OperationId == operationId),
                RabbitUtil.WaitForCqrsMessage<DepositSucceededEvent>(m => m.OperationId == operationId));

            await messagesReceivedTask;

            // assert
            (await TestsHelpers.GetAccount()).Balance.Should().Be(balanceBefore + 123);
        }
    }
}