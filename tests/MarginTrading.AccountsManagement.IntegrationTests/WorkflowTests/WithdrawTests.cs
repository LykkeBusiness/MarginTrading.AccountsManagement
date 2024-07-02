using System.Threading.Tasks;

using FluentAssertions;

using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.IntegrationTests.Infrastructure;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.IntegrationTests.WorkflowTests
{
    public class WithdrawTests
    {
        [Test]
        public async Task IfEnoughBalance_ShouldWithdraw()
        {
            // arrange
            await TestsHelpers.EnsureAccountState(needBalance: 123);

            // act

            var operationId = await ClientUtil.AccountsApi.BeginWithdraw(TestsHelpers.ClientId,
                TestsHelpers.AccountId,
                new AccountChargeManuallyRequest
                {
                    AmountDelta = 123,
                    Reason = "intergational tests: withdraw",
                });
            
            var messagesReceivedTask = Task.WhenAll(
                RabbitUtil.WaitForCqrsMessage<AccountBalanceChangedEvent>(m => m.OperationId == operationId),
                RabbitUtil.WaitForCqrsMessage<WithdrawalSucceededEvent>(m => m.OperationId == operationId));

            await messagesReceivedTask;

            // assert
            (await TestsHelpers.GetAccount()).Balance.Should().Be(0);
        }
        
        [Test]
        public async Task IfNotEnoughBalance_ShouldFailWithdraw()
        {
            // arrange
            await TestsHelpers.EnsureAccountState(needBalance: 123);
            (await TestsHelpers.GetAccount()).Balance.Should().Be(123);

            // act

            var operationId = await ClientUtil.AccountsApi.BeginWithdraw(TestsHelpers.ClientId,
                TestsHelpers.AccountId,
                new AccountChargeManuallyRequest
                {
                    AmountDelta = 124,
                    Reason = "intergational tests: withdraw",
                });
            
            var eventTask = await Task.WhenAny(
                RabbitUtil.WaitForCqrsMessage<WithdrawalSucceededEvent>(m => m.OperationId == operationId),
                RabbitUtil.WaitForCqrsMessage<WithdrawalFailedEvent>(m => m.OperationId == operationId));

            eventTask.Should().BeOfType<Task<WithdrawalFailedEvent>>();

            // assert
            (await TestsHelpers.GetAccount()).Balance.Should().Be(123);
        }
    }
}