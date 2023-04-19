// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using FsCheck;

using MarginTrading.AccountsManagement.InternalModels;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.Tests
{
    [TestFixture]
    public class OperationIdTests
    {
        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        public void Create_WhenValueIsNull_ShouldThrow(string source)
        {
            Assert.Throws<ArgumentNullException>(() => new OperationId(source));
        }
        
        [Test]
        public void Create_WhenValueIsTooLong_ShouldThrow()
        {
            var source = new string('a', OperationId.MaxLength + 1);
            
            Assert.Throws<ArgumentOutOfRangeException>(() => new OperationId(source));
        }
        
        [Test]
        public void Create_WhenValueIsCorrect_ShouldCreate()
        {
            Prop.ForAll(Gen.Choose(1, OperationId.MaxLength).ToArbitrary(), length =>
            {
                var source = new string('a', length);
                Assert.DoesNotThrow(() => new OperationId(source));
            }).QuickCheckThrowOnFailure();
        }
        
        [FsCheck.NUnit.Property]
        public Property When_StringsAreEqual_ShouldBeEqual()
        {
            return Prop.ForAll(Gen.Choose(1, OperationId.MaxLength).ToArbitrary(), length =>
            {
                var source = new string('a', length);
                var id1 = new OperationId(source);
                var id2 = new OperationId(source);
                return id1 == id2;
            });
        }
        
        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        public void Extend_WhenPostfixIsNull_ShouldThrow(string source)
        {
            var id = new OperationId("a");
            
            Assert.Throws<ArgumentNullException>(() => id.Extend(source));
        }
        
        [Test]
        public void Extend_WhenPostfixIsTooLong_ShouldThrow()
        {
            var id = new OperationId("a");
            var postfix = new string('b', OperationId.MaxLength + 1);
            
            Assert.Throws<ArgumentOutOfRangeException>(() => id.Extend(postfix));
        }
        
        [Test]
        public void Extend_When_AddingPostfix_MakesIdTooLong_ShouldThrow()
        {
            var source = new string('a', OperationId.MaxLength);
            var id = new OperationId(source);
            
            Assert.Throws<ArgumentOutOfRangeException>(() => id.Extend("b"));
        }
        
        [Test]
        public void Extend_Adds_Postix_And_Separator()
        {
            var source = new string('a', OperationId.MaxLength - 1);
            var id = new OperationId(source);
            
            Assert.Throws<ArgumentOutOfRangeException>(() => id.Extend("b"));
        }
        
        //[TestCase("whatever", "unique-postfix")]
        //[TestCase("whatever-unique-postfix", "unique-postfix")]
        //[TestCase("whatever-unique-poStfFx", "unique-postfix")]
        [Test]
        public void Extend_Is_Idempotent()
        {
            const string postfix = "unique-postfix";
            var originOperationId = new OperationId("whatever");
            var extended = originOperationId.Extend(postfix);
            var extendedTwice = extended.Extend(postfix);
            
            Assert.AreEqual(extended, extendedTwice);
        }

        [TestCase("whatever-unique-postfix", "unique-postfix")]
        [TestCase("whatever-unique-postfix", "Unique-Postfix")]
        [TestCase("whatever-unique-poStFix", "unique-postfix")]
        public void Extend_Does_Not_Add_Postfix_If_It_Is_Already_Present(string source, string postfix)
        {
            var id = new OperationId(source);

            var extended = id.Extend(postfix);
            
            Assert.AreEqual(id, extended);
        }
    }
}