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
                TestsHelpers.ClientId,
                Id = TestsHelpers.AccountId,
                IsDisabled = false,
            }, o => o.ExcludingMissingMembers());

            account.Balance.Should().BeGreaterOrEqualTo(13);
        }

        [TestCase(-10000)]
        [TestCase(10000)]
        public async Task Always_ShouldUpdateBalance(decimal delta)
        {
            // arrange
            var account = await TestsHelpers.EnsureAccountState();
            var balanceBefore = account.Balance;

            // act
            await TestsHelpers.ChargeManually(delta);

            // assert
            (await TestsHelpers.GetAccount()).Balance.Should().Be(balanceBefore + delta);
        }
    }
}