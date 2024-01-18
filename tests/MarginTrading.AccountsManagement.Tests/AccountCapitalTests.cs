// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.Snow.Common.Percents;
using MarginTrading.AccountsManagement.InternalModels;
using NUnit.Framework;

namespace MarginTrading.AccountsManagement.Tests
{
    public class AccountCapitalTests
    {
        static object[] DisposableCapitalCases =
        {
            new object[] { 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, new Percent(100), 0m },
            new object[] { 100m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, new Percent(100), 100m },
            new object[] { 100m, 50m, 0m, 0m, 0m, 0m, 0m, 0m, new Percent(100), 50m },
            new object[] { 100m, 0m, 50m, 0m, 0m, 0m, 0m, 0m, new Percent(100), 50m },
            new object[] { 100m, 0m, 0m, 50m, 0m, 0m, 0m, 0m, new Percent(100), 50m },
            new object[] { 100m, 0m, 0m, 0m, 50m, 0m, 0m, 0m, new Percent(100), 50m },
            new object[] { 100m, 0m, 0m, 0m, 0m, 50m, 0m, 0m, new Percent(100), 50m },
            new object[] { 100m, 0m, 0m, 0m, 0m, 0m, 50m, 0m, new Percent(100), 50m },
            new object[] { 100m, 0m, 0m, 0m, 0m, 0m, 0m, 50m, new Percent(100), 50m },
            new object[] { 100m, 50m, 0m, 0m, 0m, 0m, 0m, 0m, new Percent(50), 50m },
            new object[] { 100m, 0m, 50m, 0m, 0m, 0m, 0m, 0m, new Percent(50), 75m },
            new object[] { 100m, 0m, 0m, 50m, 0m, 0m, 0m, 0m, new Percent(50), 75m },
            new object[] { 100m, 0m, 0m, 0m, 50m, 0m, 0m, 0m, new Percent(50), 75m },
            new object[] { 100m, 0m, 0m, 0m, 0m, 50m, 0m, 0m, new Percent(50), 75m },
            new object[] { 100m, 0m, 0m, 0m, 0m, 0m, 50m, 0m, new Percent(50), 50m },
            new object[] { 100m, 0m, 0m, 0m, 0m, 0m, 0m, 50m, new Percent(50), 50m },
        };

        [TestCaseSource(nameof(DisposableCapitalCases))]
        public void DisposableCapitalTests(decimal totalCapital,
            decimal temporary,
            decimal deals,
            decimal compensations,
            decimal dividends,
            decimal totalUnRealisedPnl,
            decimal usedMargin,
            decimal unconfirmedMargin,
            Percent disposableCapitalWithholdPercent,
            decimal disposable)
        {
            var accountCapital = new AccountCapital(0,
                totalCapital,
                totalUnRealisedPnl,
                temporary,
                deals,
                compensations,
                dividends,
                "assetId",
                usedMargin,
                unconfirmedMargin,
                disposableCapitalWithholdPercent
                );
            
            Assert.That(accountCapital.Disposable, Is.EqualTo(disposable));
        }
    }
}