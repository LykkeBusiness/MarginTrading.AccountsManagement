// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;

using Common;

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
        public void DeserializeTemporaryCapital_When_ParameterIsIncorrect_Should_ReturnNull(string json)
        {
            //Act
            var actual = DeserializeUtils.DeserializeTemporaryCapital(json);
    
            //Assert
            Assert.IsNull(actual);
        }

        [FsCheck.NUnit.Property]
        public void DeserializeTemporaryCapital_When_ParameterIsCorrect_Should_Return_Sum(decimal[] amounts)
        {
            var json = amounts.Select(i => new { amount = i}).ToJson();
            
            //Act
            var actual = DeserializeUtils.DeserializeTemporaryCapital(json);
            var expected = amounts.Select(i => i).Sum();
            
            //Assert
            Assert.AreEqual(expected, actual);
        }
    }
}