using System;

using FluentAssertions;

using MarginTrading.AccountsManagement.InternalModels;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.Tests
{
    public class ProductComplexityWarningTests
    {
        private readonly string AccountId = "accountId";

        [Test]
        public void ShouldBeAbleToSwitchFlag()
        {
            var subject = ComplexityWarningState.Start(AccountId);

            var epoch = DateTime.UnixEpoch;

            var dtAfterFirstOrder = epoch.AddDays(1);
            subject.OnConfirmedOrderReceived(orderId: Guid.NewGuid().ToString(), dtAfterFirstOrder, 2, out var confirmationFlagSwitched);
            confirmationFlagSwitched.Should().BeFalse("After first order confirmation flag should not be switched");
            subject.ShouldShowComplexityWarning.Should().BeTrue();
            subject.SwitchedToFalseAt.Should().BeNull();
            subject.ConfirmedOrders.Count.Should().Be(1);

            var dtAfterSecondOrder = epoch.AddDays(2);
            subject.OnConfirmedOrderReceived(orderId: Guid.NewGuid().ToString(), dtAfterSecondOrder, 2, out confirmationFlagSwitched);
            confirmationFlagSwitched.Should().BeTrue("After second order confirmation flag should be switched");
            subject.ShouldShowComplexityWarning.Should().BeFalse();
            subject.SwitchedToFalseAt.Should().Be(dtAfterSecondOrder);
            subject.ConfirmedOrders.Count.Should().Be(2);

            var dtAfterThirdOrder = epoch.AddDays(3);
            subject.OnConfirmedOrderReceived(orderId: Guid.NewGuid().ToString(), dtAfterThirdOrder, 2, out confirmationFlagSwitched);
            confirmationFlagSwitched.Should().BeFalse("After all next orders confirmation  flag should be untouched");
            subject.ShouldShowComplexityWarning.Should().BeFalse();
            subject.SwitchedToFalseAt.Should().Be(dtAfterSecondOrder);
            subject.ConfirmedOrders.Count.Should().Be(3);
        }

        [Test]
        public void ShouldNotRaiseFlagChangeOnRetry()
        {
            var subject = ComplexityWarningState.Start(AccountId);

            var orderId = Guid.NewGuid().ToString();
            var cnt = 0;
            do
            {
                subject.OnConfirmedOrderReceived(orderId: orderId, DateTime.Now, 2, out var confirmationFlagSwitched);

                confirmationFlagSwitched.Should().BeFalse();
                subject.ShouldShowComplexityWarning.Should().BeTrue();
                subject.ConfirmedOrders.Count.Should().Be(1);

                cnt++;
            } while (cnt < 10);
        }


        [Test]
        public void ShouldBeAbleToResetConfirmation()
        {
            //Arrange
            var subject = ComplexityWarningState.Start(AccountId);

            var cnt = 0;
            do
            {
                subject.OnConfirmedOrderReceived(Guid.NewGuid().ToString(), DateTime.Now, 2, out var _);
                cnt++;
            } while (cnt < 10);

            subject.ShouldShowComplexityWarning.Should().BeFalse();
            subject.SwitchedToFalseAt.Should().NotBeNull();

            //Act
            subject.ResetConfirmation();

            //Assert
            subject.ShouldShowComplexityWarning.Should().BeTrue();
            subject.SwitchedToFalseAt.Should().BeNull();
            subject.ConfirmedOrders.Should().BeEmpty();
        }
    }
}
