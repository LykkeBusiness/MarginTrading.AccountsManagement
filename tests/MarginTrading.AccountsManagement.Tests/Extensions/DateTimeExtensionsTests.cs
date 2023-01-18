// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MarginTrading.AccountsManagement.Extensions;
using MarginTrading.AccountsManagement.Repositories.Implementation.SQL;
using MarginTrading.AccountsManagement.Tests.Helpers;
using NUnit.Framework;

namespace MarginTrading.AccountsManagement.Tests.Extensions
{
    public class DateTimeExtensionsTests
    {
        private static object[] AccountsTestData = new object[]
        {
            new List<AccountEntity>() 
            {
                new AccountEntity() { ModificationTimestamp = DateTime.UtcNow.AddDays(-5), LastUpdatedAt = DateTime.UtcNow },
                new AccountEntity() { ModificationTimestamp = DateTime.UtcNow.AddDays(2), LastUpdatedAt = DateTime.UtcNow },
                new AccountEntity() { ModificationTimestamp = new DateTime(2023, 01, 20, 10, 0, 0), LastUpdatedAt = new DateTime(2023, 01, 20, 10, 11, 0)},
                new AccountEntity() { ModificationTimestamp = new DateTime(2023, 01, 20, 10, 1, 0), LastUpdatedAt = new DateTime(2023, 01, 20, 10, 0, 0)}
            }
        };

        private FakeLocalTimeZone _fakeLocalTimeZone; 
        
        [SetUp]
        public void SetUp()
        {
            var timeZoneInfo = TimeZoneInfo.GetSystemTimeZones()
                .ToList()
                .Where(x => x.BaseUtcOffset.Hours == 5)
                .First();
            _fakeLocalTimeZone = new FakeLocalTimeZone(timeZoneInfo);
        }
        
        [TearDown]
        public void TearDown()
        {
            _fakeLocalTimeZone.Dispose();
        }
        
        [Test]
        public void AssumeUtcIfUnspecified_UnspecifiedKind_SpecifiesUtc()
        {
            //arrange
            var date = new DateTime(2022, 02, 15, 10, 30, 0);
            var dateTime = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);

            //act
            var result = dateTime.AssumeUtcIfUnspecified();

            //assert
            result.Kind.Should().Be(DateTimeKind.Utc);
            result.Should().HaveHour(10);
        }
        
        [Test]
        public void AssumeUtcIfUnspecified_UtcKind_RemainsTheSame()
        {
            //arrange
            var date = new DateTime(2022, 02, 15, 10, 30, 0);
            var dateTime = DateTime.SpecifyKind(date, DateTimeKind.Utc);

            //act
            var result = dateTime.AssumeUtcIfUnspecified();

            //assert
            result.Kind.Should().Be(DateTimeKind.Utc);
            result.Should().HaveHour(10);
        }
        
        [Test]
        public void AssumeUtcIfUnspecified_LocalKind_ConvertsToUtc()
        {
            //arrange
            var date = new DateTime(2022, 02, 15, 10, 30, 0);
            var dateTime = DateTime.SpecifyKind(date, DateTimeKind.Local);
            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);

            //act
            var result = dateTime.AssumeUtcIfUnspecified();

            //assert
            result.Kind.Should().Be(DateTimeKind.Utc);
            offset.Should().NotBe(TimeSpan.Zero); // only for testing purposes
            result.Should().Be(date.Add(-offset));
        }

        [Test]
        [TestCase("2023-01-18T10:00:00+0000", "2023-01-18T11:00:00+0000", "2023-01-18T11:00:00+0000")]
        [TestCase("2023-01-19T10:00:00+0000", "2023-01-18T11:00:00+0000", "2023-01-19T10:00:00+0000")]
        [TestCase("2023-01-18T10:29:00+0000", "2023-01-18T10:31:00+0000", "2023-01-18T10:31:00+0000")]
        [TestCase("2023-01-19T10:42:00+0000", "2023-01-19T10:39:00+0000", "2023-01-19T10:42:00+0000")]
        [TestCase("2023-01-20T10:00:00+0300", "2023-01-20T11:00:00+0500", "2023-01-20T10:00:00+0300")]
        [TestCase("2023-01-20T11:30:00+0300", "2023-01-20T13:29:00+0500", "2023-01-20T11:30:00+0300")]
        public void MaxDateTimeFunction_ShouldReturnTheMostRecentDateTime(string timestamp1, string timestamp2, string expected)
        {
            var dt1 = DateTime.Parse(timestamp1);
            var dt2 = DateTime.Parse(timestamp2);

            var result = DateTimeExtensions.MaxDateTime(dt1, dt2);

            Assert.AreEqual(DateTime.Parse(expected), result);
        }

        [Test]
        [TestCaseSource(nameof(AccountsTestData))]
        public void OverwriteTimestampWithMostRecent_ShouldPickupTheMostRecentTimestamp(IEnumerable<AccountEntity> accounts)
        {
            // Arrange
            Action<AccountEntity, DateTime> fnUpdateDateTimeProperty = (account, mostRecent) => account.ModificationTimestamp = mostRecent;

            // Act
            DateTimeExtensions.OverwriteTimestampWithMostRecent(accounts, 
                getFirstTimeStamp: (a) => a.LastUpdatedAt,
                getSecondTimestamp: (a) => a.ModificationTimestamp,
                updateProperty: fnUpdateDateTimeProperty);

            // Assert
            foreach(var account in accounts)
            {
                DateTime greater = default(DateTime);

                if (account.ModificationTimestamp >= account.LastUpdatedAt)
                    greater = account.ModificationTimestamp;
                else
                    greater = account.LastUpdatedAt;

                Assert.AreEqual(greater, account.ModificationTimestamp);
            }
        }
    }
}