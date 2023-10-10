// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using MarginTrading.AccountsManagement.Services.Implementation;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.Tests
{
    public sealed class AccountsApiDecoratorTests
    {
        [Test]
        public void ExtractOfType_When_NullInput_RaisesException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AccountsApiDecorator.ExtractOfType<object>(null, "key"));
        }

        [Test]
        public void ExtractOfType_When_EmptyInput_ReturnsDefault()
        {
            var result = AccountsApiDecorator.ExtractOfType<object>(new Dictionary<object, object>(), "key");
            Assert.AreEqual(default, result);
        }
        
        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void ExtractOfType_When_KeyIsInvalid_RaisesException(string invalidKey)
        {
            var nonEmptyDictionary = new Dictionary<object, object> { { "1", new object() } };
            
            Assert.Throws<ArgumentNullException>(() =>
                AccountsApiDecorator.ExtractOfType<object>(nonEmptyDictionary, invalidKey));
        }
        
        [Test]
        public void ExtractOfType_When_NoKeyPresent_ReturnsDefault()
        {
            var nonEmptyDictionary = new Dictionary<object, object> { { "1", new object() } };
            
            var result = AccountsApiDecorator.ExtractOfType<object>(nonEmptyDictionary, "2");
            
            Assert.AreEqual(default, result);
        }
        
        [Test]
        public void ExtractOfType_When_KeyPresentButValueIsOfDifferentType_RaisesException()
        {
            var nonEmptyDictionary = new Dictionary<object, object> { { "1", new object() } };

            Assert.Throws<InvalidCastException>(() =>
                AccountsApiDecorator.ExtractOfType<string>(nonEmptyDictionary, "1"));
        }
        
        [Test]
        public void ExtractOfType_When_KeyPresent_ReturnsValue()
        {
            var expected = new object();
            var nonEmptyDictionary = new Dictionary<object, object> { { "1", expected } };

            var result = AccountsApiDecorator.ExtractOfType<object>(nonEmptyDictionary, "1");
            
            Assert.AreEqual(expected, result);
        }
    }
}