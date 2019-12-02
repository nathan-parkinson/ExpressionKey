using ExpressionKey.Visitors;
using NUnit.Framework;
using System;
using System.Linq.Expressions;

namespace ExpressionKeyTests
{
    public class ParameterReplacerTests
    {
        [Test]
        public void ParameterUsedMultipleTimesHasAllReferencesReplacedTest()
        {
            Expression<Func<TestClass, int>> expr = p => (p.Id * 3) + (p.ParentId.Value * 2);
            var newParam = Expression.Parameter(typeof(TestClass), "newParam");
            var result = ParameterReplacer.Replace(expr.Body, expr.Parameters[0], newParam);

            var lambda = Expression.Lambda<Func<TestClass, int>>(result, newParam);
            var func = lambda.Compile();

            var answer = func(new TestClass { Id = 5, ParentId = 3 });

            Assert.AreEqual(answer, 21);
            Assert.AreEqual(lambda.Parameters[0], newParam);
        }

        [Test]
        public void ParameterUsedMultipleTimesWithAnotherParameterHasAllReferencesReplacedTest()
        {
            Expression<Func<TestClass, TestClass, int>> expr = (p, p2) => p2.Id + (p.Id * 3) + (p.ParentId.Value * 2);
            var newParam = Expression.Parameter(typeof(TestClass), "newParam");
            var result = ParameterReplacer.Replace(expr.Body, expr.Parameters[0], newParam);

            var lambda = Expression.Lambda<Func<TestClass, TestClass, int>>(result, newParam, expr.Parameters[1]);
            var func = lambda.Compile();

            var answer = func(new TestClass { Id = 5, ParentId = 3 }, new TestClass { Id = 20 });

            Assert.AreEqual(answer, 41);
            Assert.AreEqual(lambda.Parameters[0], newParam);
        }

        public class TestClass
        {
            public int Id { get; set; }
            public int? ParentId { get; set; }
            public string Name { get; set; }
        }
    }
}