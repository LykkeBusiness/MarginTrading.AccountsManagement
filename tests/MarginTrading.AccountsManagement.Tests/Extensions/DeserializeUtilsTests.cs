// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
        
        [TestCase("[]", 0)]
        [TestCase("[{}]", 0)]
        [TestCase("[{}, {}]", 0)]
        [TestCase("[{'Amount': 1}]", 1)]
        [TestCase("[{'amount': 1}]", 1)]
        [TestCase("[{'amount': 1}]", 1)]
        [TestCase("[{'amount': 1}, {'amount': 2}]", 3)]
        [TestCase("[{'whatever': 'any', 'amount': 1}, {'amount': 2}]", 3)]
        [TestCase("[{'amount': 1}, {'amount': 2}, {'amount': -5}]", -2)]
        public void DeserializeTemporaryCapital_When_ParameterIsCorrect_Should_Return_Sum(string json, decimal expected)
        {
            //Act
            var actual = DeserializeUtils.DeserializeTemporaryCapital(json);
    
            //Assert
            Assert.AreEqual(expected, actual);
        }
    }
}