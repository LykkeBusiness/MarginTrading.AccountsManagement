using System;
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
            var account = await TestsHelpers.EnsureAccountState(needBalance: 123);
            var balanceBefore = account.Balance;
        
            // act

            var response = await ClientUtil.AccountsApi.TryBeginWithdraw(
                TestsHelpers.AccountId,
                new AccountChargeRequest
                {
                    OperationId = Guid.NewGuid().ToString("N"),
                    AmountDelta = 123,
                    Reason = "intergational tests: withdraw",
                });
            
            response.Error.Should().Be(WithdrawalErrorContract.None);
            
            var messagesReceivedTask = Task.WhenAll(
                RabbitUtil.WaitForCqrsMessage<AccountChangedEvent>(m => m.OperationId == response.OperationId),
                RabbitUtil.WaitForCqrsMessage<WithdrawalSucceededEvent>(m => m.OperationId == response.OperationId));
        
            await messagesReceivedTask;
        
            // assert
            (await TestsHelpers.GetAccount()).Balance.Should().Be(balanceBefore - 123);
        }
        
        [Test]
        public async Task IfNotEnoughBalance_ShouldFailWithdraw()
        {
            // arrange
            var account = await TestsHelpers.EnsureAccountState(needBalance: 1);
            var balanceBefore = account.Balance;
        
            // act
            var response = await ClientUtil.AccountsApi.TryBeginWithdraw(
                TestsHelpers.AccountId,
                new AccountChargeManuallyRequest
                {
                    OperationId = Guid.NewGuid().ToString("N"),
                    AmountDelta = account.Balance + 1,
                    Reason = "intergational tests: withdraw",
                });

            response.Error.Should().Be(WithdrawalErrorContract.None);

            var succeededTask = RabbitUtil.WaitForCqrsMessage<WithdrawalSucceededEvent>(m => m.OperationId == response.OperationId);
            var failedTask = RabbitUtil.WaitForCqrsMessage<WithdrawalFailedEvent>(m => m.OperationId == response.OperationId);
            var resultTask = await Task.WhenAny(succeededTask, failedTask);

            resultTask.Should().Be(failedTask);
        
            // assert
            (await TestsHelpers.GetAccount()).Balance.Should().Be(balanceBefore);
        }
    }
}