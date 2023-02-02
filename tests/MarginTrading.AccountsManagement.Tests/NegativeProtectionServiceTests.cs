// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using static System.Math;

using FluentAssertions;

using FsCheck;

using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Services.Implementation;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AccountsManagement.Tests.Fakes;

using Microsoft.Extensions.Internal;

namespace MarginTrading.AccountsManagement.Tests
{
    public class NegativeProtectionServiceTests
    {
        [FsCheck.NUnit.Property]
        public Property Check_WhenPositiveOrZeroNewBalance_ShouldReturnNull()
        {
            var sut = CreateSut();

            return Prop.ForAll(
                (from balance in Arb.Default.NonNegativeInt().Generator
                    from change in Arb.Default.Decimal().Generator
                    select (balance: balance.Item, change: change)).ToArbitrary(), t =>
                {
                    sut.CheckAsync("operationId", "accountId", t.balance, t.change).Result.Should().BeNull();
                });
        }

        [FsCheck.NUnit.Property]
        public Property Check_WhenPositiveChangeAmount_ShouldReturnNull(NonNegativeInt changeAmount)
        {
            var sut = CreateSut();

            return Prop.ForAll(
                (from balance in Arb.Default.NegativeInt().Generator
                    from change in Arb.Default.PositiveInt().Generator
                    select (balance: balance.Item, change: change.Item)).ToArbitrary(), t =>
                {
                    sut.CheckAsync("operationId", "accountId", t.balance, t.change).Result.Should().BeNull();
                });
        }
        
        [FsCheck.NUnit.Property]
        public Property Check_WhenBalanceWasNegativeBeforeChange_ShouldCompensateOnlyChangeAmount()
        {
            var sut = CreateSut();

            return Prop.ForAll(
                (from change in Arb.Default.NegativeInt().Generator
                    from newBalance in Gens.LessThan(change.Get)
                    select (newBalance: newBalance, change: change.Item)).ToArbitrary(), t =>
                {
                    var compensationAmount = sut.CheckAsync("operationId", "accountId", t.newBalance, t.change).Result;

                    compensationAmount.Should().Be(Abs(t.change));
                });
        }
        
        [FsCheck.NUnit.Property]
        public Property Check_WhenBalanceWasPositiveBeforeChange_ShouldCompensateTheDifference()
        {
            var sut = CreateSut();

            return Prop.ForAll(
                (from change in Arb.Default.NegativeInt().Generator
                    from newBalance in Gens.BetweenInclusive(change.Get, -1)
                    select (newBalance: newBalance, change: change.Item)).ToArbitrary(), t =>
                {
                    var compensationAmount = sut.CheckAsync("operationId", "accountId", t.newBalance, t.change).Result;

                    compensationAmount.Should().Be(Abs(t.newBalance));
                });
        }
        
        [FsCheck.NUnit.Property]
        public Property Check_WhenAutoCompensationIsEnabled_ShouldCompensate()
        {
            return Prop.ForAll(
                (from change in Arb.Default.NegativeInt().Generator
                    from newBalance in Gens.LessThan(change.Get)
                    select (newBalance: newBalance, change: change.Item)).ToArbitrary(), t =>
                {
                    var sendBalanceCommandsService = new FakeSendBalanceCommandsService();
                    var sut = CreateSut(sendBalanceCommandsService, true);
                    
                    var compensationAmount = sut.CheckAsync("operationId", "accountId", t.newBalance, t.change).Result;

                    compensationAmount.Should().NotBeNull();
                    sendBalanceCommandsService.ChargeManuallyAsyncWasCalled.Should().BeTrue();
                });
        }

        [FsCheck.NUnit.Property]
        public Property Check_When_AccountIsInLiquidation_ShouldNotCompensate()
        {
            var sendBalanceCommandsService = new FakeSendBalanceCommandsService();
            var sut = new NegativeProtectionService(sendBalanceCommandsService,
                new SystemClock(),
                new AccountManagementSettings { NegativeProtectionAutoCompensation = true },
                // emulating core requests which return any account with IsInLiquidation = true
                new AlwaysInLiquidationAccountApi());

            return Prop.ForAll(
                (from change in Arb.Default.NegativeInt().Generator
                    from newBalance in Gens.LessThan(change.Get)
                    select (newBalance: newBalance, change: change.Item)).ToArbitrary(), t =>
                {
                    var compensationAmount = sut.CheckAsync("operationId", "accountId", t.newBalance, t.change).Result;

                    // compensation amount has to be calculated
                    compensationAmount.Should().NotBeNull();
                    // but not charged
                    sendBalanceCommandsService.ChargeManuallyAsyncWasCalled.Should().BeFalse();
                });
        }

        private INegativeProtectionService CreateSut(ISendBalanceCommandsService sendBalanceCommandsService = null,
            bool negativeProtectionAutoCompensation = false)
        {
            return new NegativeProtectionService(sendBalanceCommandsService ?? new FakeSendBalanceCommandsService(),
                new SystemClock(),
                new AccountManagementSettings
                {
                    NegativeProtectionAutoCompensation = negativeProtectionAutoCompensation
                },
                new FakeAccountsApi());
        }
    }
}