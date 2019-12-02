using ExpressionKey.Visitors;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionKeyTests
{
    public class EntityMatchWalkerTests
    {
        [Test]
        public void TwoEqualsAndBetweenTest()
        {
            Expression<Func<Person, Child, bool>> ex =
                (p, c) => p.Id1 == c.ParentId1 && p.Id2 == c.ParentId2 &&
                            p.DOB >= c.DateStart && p.DOB <= c.DateEnd;

            var result = EntityMatchWalker.ExtractKeyNodes(ex, ex.Parameters[0], ex.Parameters[1]);
            Assert.AreEqual(2, result.Item1.Count);
            Assert.AreEqual(2, result.Item2.Count);

            Assert.AreEqual(typeof(Person).GetProperty(nameof(Person.Id1)), ((MemberExpression)result.Item1[0]).Member);
            Assert.AreEqual(typeof(Person).GetProperty(nameof(Person.Id2)), ((MemberExpression)result.Item1[1]).Member);
            Assert.AreEqual(typeof(Child).GetProperty(nameof(Child.ParentId1)), ((MemberExpression)result.Item2[0]).Member);
            Assert.AreEqual(typeof(Child).GetProperty(nameof(Child.ParentId2)), ((MemberExpression)result.Item2[1]).Member);

        }

        [Test]
        public void EqualsWithMethodTest()
        {
            Expression<Func<Person, Child, bool>> ex =
                (p, c) => p.Name.First() == c.ParentId1.ToString().First();

            var result = EntityMatchWalker.ExtractKeyNodes(ex, ex.Parameters[0], ex.Parameters[1]);
            Assert.AreEqual(1, result.Item1.Count);
            Assert.AreEqual(1, result.Item2.Count);

            var func1 = Expression.Lambda<Func<Person, int>>(result.Item1.First(), ex.Parameters[0]).Compile();
            Assert.AreEqual('W', func1(new Person { Name = "William" }));

            var func2 = Expression.Lambda<Func<Child, int>>(result.Item2.First(), ex.Parameters[1]).Compile();
            Assert.AreEqual('1', func2(new Child { ParentId1 = 1234 }));
        }

        [Test]
        public void ComplexEqualsTest()
        {
            Expression<Func<Person, Child, bool>> ex =
                (p, c) => p.Id1 + 3 == c.ParentId1 + 6;

            var result = EntityMatchWalker.ExtractKeyNodes(ex, ex.Parameters[0], ex.Parameters[1]);
            Assert.AreEqual(1, result.Item1.Count);
            Assert.AreEqual(1, result.Item2.Count);

            var func1 = Expression.Lambda<Func<Person, int>>(result.Item1.First(), ex.Parameters[0]).Compile();
            Assert.AreEqual(8, func1(new Person { Id1 = 5}));

            var func2 = Expression.Lambda<Func<Child, int>>(result.Item2.First(), ex.Parameters[1]).Compile();
            Assert.AreEqual(16, func2(new Child { ParentId1 = 10 }));
        }


        [Test]
        public void ComplexUnusableOrTest()
        {
            Expression<Func<Person, Child, bool>> ex =
                (p, c) => (p.Id1 == c.ParentId1 && p.Id2 == c.ParentId2 &&
                p.DOB >= c.DateStart && p.DOB <= c.DateEnd) || p.DOB == DateTime.MinValue;

            var result = EntityMatchWalker.ExtractKeyNodes(ex, ex.Parameters[0], ex.Parameters[1]);
            Assert.AreEqual(0, result.Item1.Count);
            Assert.AreEqual(0, result.Item2.Count);
        }

        [Test]
        public void SimpleUnusableOrTest()
        {
            Expression<Func<Person, Child, bool>> ex =
                (p, c) => p.Id1 == c.ParentId1 || p.DOB == DateTime.MinValue;

            var result = EntityMatchWalker.ExtractKeyNodes(ex, ex.Parameters[0], ex.Parameters[1]);
            Assert.AreEqual(0, result.Item1.Count);
            Assert.AreEqual(0, result.Item2.Count);
        }

        [Test]
        public void UsableOrTest()
        {
            Expression<Func<Person, Child, bool>> ex =
                (p, c) => p.Id1 == c.ParentId1 &&
                        (p.DOB == DateTime.MinValue || p.DOB == DateTime.MaxValue);

            var result = EntityMatchWalker.ExtractKeyNodes(ex, ex.Parameters[0], ex.Parameters[1]);
            Assert.AreEqual(1, result.Item1.Count);
            Assert.AreEqual(1, result.Item2.Count);
        }

        [Test]
        public void NotNotEqualTest()
        {
            Expression<Func<Person, Child, bool>> ex =
                (p, c) => !(p.Id1 != c.ParentId1);

            var result = EntityMatchWalker.ExtractKeyNodes(ex, ex.Parameters[0], ex.Parameters[1]);
            Assert.AreEqual(1, result.Item1.Count);
            Assert.AreEqual(1, result.Item2.Count);
        }

        [Test]
        public void ComplexNotNotEqualTest1()
        {
            Expression<Func<Person, Child, bool>> ex =
                (p, c) => !(p.Id1 != c.ParentId1) && (p.DOB == DateTime.MinValue || p.DOB == DateTime.MaxValue);

            var result = EntityMatchWalker.ExtractKeyNodes(ex, ex.Parameters[0], ex.Parameters[1]);
            Assert.AreEqual(1, result.Item1.Count);
            Assert.AreEqual(1, result.Item2.Count);
        }

        [Test]
        public void ComplexNotNotEqualTest2()
        {
            Expression<Func<Person, Child, bool>> ex =
                (p, c) => !(p.Id1 + 4 != c.ParentId1 + 9) && (p.DOB == DateTime.MinValue || p.DOB == DateTime.MaxValue);

            var result = EntityMatchWalker.ExtractKeyNodes(ex, ex.Parameters[0], ex.Parameters[1]);
            Assert.AreEqual(1, result.Item1.Count);
            Assert.AreEqual(1, result.Item2.Count);
        }


        public class Person
        {
            public int Id1 { get; set; }
            public int Id2 { get; set; }
            public string Name { get; set; }

            public DateTime DOB { get; set; }

            public List<Child> Children { get; set; } = new List<Child>();
                
        }

        public class Child
        {
            public int ChildId { get; set; }
            public int ParentId1 { get; set; }
            public int ParentId2 { get; set; }
            public DateTime DateStart { get; set; }
            public DateTime DateEnd { get; set; }
        }
    }
}