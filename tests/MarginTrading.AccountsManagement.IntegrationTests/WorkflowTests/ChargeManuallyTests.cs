using System.Threading.Tasks;

using FluentAssertions;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.IntegrationTests.WorkflowTests
{
    public class ChargeManuallyTests
    {
        [Test]
        public async Task EnsureAccountState_Always_ShouldFixAccount()
        {
            // arrange

            // act
            var result = await TestsHelpers.EnsureAccountState(needBalance: 13);

            // assert
            var account = await TestsHelpers.GetAccount();
            account.Should().BeEquivalentTo(new
            {
                ClientId = TestsHelpers.ClientId,
                Id = TestsHelpers.AccountId,
                Balance = 13,
                IsDisabled = false,
            }, o => o.ExcludingMissingMembers());
            
            result.Should().BeEquivalentTo(account);
        }

        [TestCase(-10000)]
        [TestCase(10000)]
        public async Task Always_ShouldUpdateBalance(decimal delta)
        {
            // arrange
            await TestsHelpers.EnsureAccountState();

            // act
            await TestsHelpers.ChargeManually(delta);

            // assert
            (await TestsHelpers.GetAccount()).Balance.Should().Be(0 + delta);
        }
    }
}