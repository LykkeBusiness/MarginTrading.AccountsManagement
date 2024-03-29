// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using FluentAssertions;
using MarginTrading.AccountsManagement.Extensions;
using MarginTrading.AccountsManagement.Tests.Helpers;
using NUnit.Framework;

namespace MarginTrading.AccountsManagement.Tests.Extensions
{
    public class DateTimeExtensionsTests
    {
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
    }
}