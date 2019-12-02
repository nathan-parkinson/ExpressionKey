using NUnit.Framework;
using System;
using System.Linq.Expressions;
using ExpressionKey.Visitors;
using System.Linq;

namespace ExpressionKeyTests
{
    public class ParameterExtractorTests
    {
        [Test]
        public void ExtractParametersMultipleOfSameTypeTest()
        {
            Expression<Func<TestClass, TestClass, int>> expr = (t, t1) => t.Id + t1.Id;

            var result = ParameterExtractor.ExtractParameters(expr.Body);

            Assert.AreEqual(result.Count, 2);
            Assert.AreEqual(result.First(), expr.Parameters[0]);
            Assert.AreEqual(result.Skip(1).First(), expr.Parameters[1]);

        }

        [Test]
        public void ExtractParametersMultipleOfDifferentTypesTest()
        {
            Expression<Func<TestClass, TestClass2, int>> expr = (t, t1) => t.Id + t1.TestClass2Id;

            var result = ParameterExtractor.ExtractParameters(expr.Body);

            Assert.AreEqual(result.Count, 2);
            Assert.AreEqual(result.First(), expr.Parameters[0]);
            Assert.AreEqual(result.Skip(1).First(), expr.Parameters[1]);
        }

        [Test]
        public void ExtractParametersSingleTest()
        {
            Expression<Func<TestClass, int>> expr = t => t.Id;

            var result = ParameterExtractor.ExtractParameters(expr.Body);

            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result.First(), expr.Parameters[0]);
        }

        [Test]
        public void ExtractParametersNoneTest()
        {
            Expression<Func<int>> expr = () => 12;
            var result = ParameterExtractor.ExtractParameters(expr.Body);

            Assert.AreEqual(result.Count, 0);
        }

        [Test]
        public void ExtractParametersSameParameterUsedMultipleTimesTest()
        {
            Expression<Func<TestClass, int>> expr = t => t.Id + t.ParentId.Value;

            var result = ParameterExtractor.ExtractParameters(expr.Body);

            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result.First(), expr.Parameters[0]);
        }


        public class TestClass
        {
            public int Id { get; set; }
            public int? ParentId { get; set; }
            public string Name { get; set; }
        }


        public class TestClass2
        {
            public int TestClass2Id { get; set; }
        }
    }
}