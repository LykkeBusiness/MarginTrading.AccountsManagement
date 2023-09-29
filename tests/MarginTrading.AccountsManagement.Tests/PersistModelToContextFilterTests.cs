// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using MarginTrading.AccountsManagement.Filters;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.Tests
{
    public sealed class PersistModelToContextFilterTests
    {
        [Test]
        public void ExtractFirstOfType_When_NullInput_RaisesException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                PersistModelToContextFilter<object>.ExtractFirstOfType<object>(null));
        }
        
        [Test]
        public void ExtractFirstOfType_When_EmptyInput_ReturnsDefault()
        {
            var result = PersistModelToContextFilter<object>.ExtractFirstOfType<object>(Array.Empty<object>());
            Assert.AreEqual(default, result);
        }
        
        [Test]
        public void ExtractOfType_When_NoValueOfTypePreset_ReturnsDefault()
        {
            var result = PersistModelToContextFilter<decimal>.ExtractFirstOfType<decimal>(
                    new object[] { 1, "not an object type" });
            Assert.AreEqual(default(decimal), result);
        }
        
        [Test]
        [Repeat(10)]
        public void ExtractOfType_When_MultipleValuesOfTypePresent_ReturnsFirst()
        {
            var result = PersistModelToContextFilter<int>.ExtractFirstOfType<int>(
                new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            Assert.AreEqual(1, result);
        }
        
        [Test]
        public void TryPersist_When_StoreIsNull_RaisesException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                PersistModelToContextFilter<object>.TryPersist(null, "key", new object()));
        }
        
        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void TryPersist_When_KeyIsInvalid_RaisesException(string invalidKey)
        {
            Assert.Throws<ArgumentNullException>(() =>
                PersistModelToContextFilter<object>.TryPersist(new Dictionary<object, object>(), invalidKey,
                    new object()));
        }
        
        public void TryPersist_When_Add_Succeeds_ReturnsTrue()
        {
            var store = new Dictionary<object, object>();
            var key = "key";
            var value = new object();
            
            var result = PersistModelToContextFilter<object>.TryPersist(store, key, value);
            
            Assert.IsTrue(result);
            Assert.AreEqual(value, store[key]);
        }
        
        [Test]
        public void TryPersist_When_Add_Fails_ReturnsFalse()
        {
            var store = new Dictionary<object, object>();
            var key = "key";
            var value = new object();
            
            store.Add(key, value);

            var sameKey = key;
            var anotherValue = new object();
            
            var result = PersistModelToContextFilter<object>.TryPersist(store, sameKey, anotherValue);
            
            Assert.IsFalse(result);
            Assert.AreEqual(1, store.Count);
        }
    }
}