// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

using Common;

using FluentAssertions;

using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Repositories.Implementation;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.Tests.Extensions
{
    public class DeserializeUtilsTests
    {
        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        [TestCase("invalid json")]
        [TestCase("[{'amount': 'invalid decimal'}]")]
        [TestCase("[{'amount': null}]")]
        [TestCase("[{'amount': 1}, {'amount': 'invalid decimal'}]")]
        public void DeserializeTemporaryCapital_When_ParameterIsIncorrect_Should_ReturnEmpty(string json)
        {
            //Act
            var actual = DeserializeUtils.DeserializeTemporaryCapital(json);
    
            //Assert
            actual.Should().BeEmpty();
        }

        [FsCheck.NUnit.Property]
        public void DeserializeTemporaryCapital_When_ParameterIsCorrect_Should_Return_Array(decimal[] amounts)
        {
            var json = amounts.Select(i => new { amount = i}).ToJson();
            
            //Act
            var actual = DeserializeUtils.DeserializeTemporaryCapital(json);
            var expected = amounts.Select(i => i).Sum();
            
            //Assert
            actual.Select(x => x.Amount).Except(amounts).Should().BeEmpty();
            Assert.AreEqual(amounts.Length, actual.Count);
        }
        
        [FsCheck.NUnit.Property]
        public void SummarizeTemporaryCapital_When_ParameterIsCorrect_Should_Return_Sum(List<decimal> amounts)
        {
            //Act
            var temporaryCapital = amounts.Select(x => new TemporaryCapital { Amount = x }).ToList();
            var actual = temporaryCapital.Summarize();

            //Assert
            Assert.AreEqual(amounts.Sum(), actual);
        }
    }
}