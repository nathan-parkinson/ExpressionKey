using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionKey;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;

namespace ExpressionKeyTests
{
    public class EntityPoolTests
    {
        [Test]
        public void EntityPoolTest()
        {
            var people = Enumerable.Range(1, 1000).Select(z => new Order
            {
                OrderId = z,
                OrderDate = DateTime.Today.AddDays(z)
            }).ToList();

            var children = Enumerable.Range(1, 10000).Select(z => new ProductLine
            {
                OrderId = (int)Math.Ceiling(z / 10.0f),
                ProductCode = "Child " + z,
                ProductLineId = z
            }).ToList();


            var pool = new TestBuilder().CreateEntityPool();
            pool.AddEntities(people);
            pool.AddEntities(children);


            Assert.IsTrue(people.All(x => x.ProductLines.All(c => c.OrderId == x.OrderId)));
            Assert.IsTrue(children.All(c => c.Order.OrderId == c.OrderId));
        }


        [Test]
        public void MatchFromBaseTypeRelationshipTest()
        {
            var people = Enumerable.Range(1, 10).Select(z => new Order1
            {
                OrderId = z,
                OrderDate = DateTime.Today.AddDays(z)
            }).ToList();

            var children = Enumerable.Range(1, 100).Select(z => new ProductLine
            {
                OrderId = (int)Math.Ceiling(z / 10.0f),
                ProductCode = "Child " + z,
                ProductLineId = z
            }).ToList();


            var pool = new TestBuilder().CreateEntityPool();
            pool.AddEntities(people);
            pool.AddEntities(children);


            Assert.IsTrue(people.All(x => x.ProductLines.All(c => c.OrderId == x.OrderId)));
            Assert.IsTrue(children.All(c => c.Order.OrderId == c.OrderId));
        }


        [Test]
        public void MatchFromTopLevelTypeRelationshipTest()
        {
            var people = Enumerable.Range(1, 10).Select(z => new Order1
            {
                OrderId = z,
                OrderDate = DateTime.Today.AddDays(z)
            }).ToList();

            var links = Enumerable.Range(1, 10).Select(z => new LinkedClass1
            {
                OrderId = z,
                Id = z * 2
            }).ToList();


            var children = Enumerable.Range(1, 100).Select(z => new ProductLine
            {
                OrderId = (int)Math.Ceiling(z / 10.0f),
                ProductCode = "Child " + z,
                ProductLineId = z
            }).ToList();


            var pool = new TestBuilder().CreateEntityPool();
            pool.AddEntities(people);
            pool.AddEntities(children);
            pool.AddEntities(links);

            Assert.IsTrue(people.All(x => x.ProductLines.All(c => c.OrderId == x.OrderId)));
            Assert.IsTrue(children.All(c => c.Order.OrderId == c.OrderId));
            Assert.IsTrue(people.All(c => c.Link1.OrderId == c.OrderId));
        }


        [Test]
        public void MatchFromMidLevelTypeRelationshipTest()
        {
            var people = Enumerable.Range(1, 10).Select(z => new Order2
            {
                OrderId = z,
                OrderDate = DateTime.Today.AddDays(z)
            }).ToList();

            var links1 = Enumerable.Range(1, 10).Select(z => new LinkedClass2
            {
                OrderId = z,
                Id = z * 2
            }).ToList();


            var links2 = Enumerable.Range(1, 10).Select(z => new LinkedClass1
            {
                OrderId = z,
                Id = z * 2
            }).ToList();


            var children = Enumerable.Range(1, 100).Select(z => new ProductLine
            {
                OrderId = (int)Math.Ceiling(z / 10.0f),
                ProductCode = "Child " + z,
                ProductLineId = z
            }).ToList();


            var pool = new TestBuilder().CreateEntityPool();
            pool.AddEntities(people);
            pool.AddEntities(children);
            pool.AddEntities(links1);
            pool.AddEntities(links2);

            Assert.IsTrue(people.All(x => x.ProductLines.All(c => c.OrderId == x.OrderId)));
            Assert.IsTrue(children.All(c => c.Order.OrderId == c.OrderId));
            Assert.IsTrue(people.All(c => c.Link1.OrderId == c.OrderId));
            Assert.IsTrue(people.All(c => c.Link2.OrderId == c.OrderId));
        }

        [Test]
        public void MatchFromTopLevelTypeRelationshipUsingBaseTypeFromPolymorphicListTest()
        {
            var people = Enumerable.Range(1, 10).Select(z => new Order1
            {
                OrderId = z,
                OrderDate = DateTime.Today.AddDays(z)
            }).ToList();

            var links = Enumerable.Range(1, 10).Select(z => new LinkedClass1
            {
                OrderId = z,
                Id = z * 2
            }).ToList();

            var pool = new TestBuilder().CreateEntityPool();
            pool.AddEntities(people.OfType<Order>());
            pool.AddEntities(links);

            Assert.IsTrue(people.All(x => x.ProductLines.All(c => c.OrderId == x.OrderId)));
            Assert.IsTrue(people.All(c => c.Link1.OrderId == c.OrderId));
        }





        public class LinkedClass1 { public int Id { get; set; } public int OrderId { get; set; } }
        public class LinkedClass2 { public int Id { get; set; } public int OrderId { get; set; } }
        public class LinkedClass3 { public int Id { get; set; } public int OrderId { get; set; } }


        public class Order2 : Order1
        {
            public LinkedClass2 Link2 { get; set; }
        }



        public class Order1 : Order
        {
            public LinkedClass1 Link1 { get; set; }
        }

        public class Order
        {
            public int OrderId { get; set; }
            public DateTime OrderDate { get; set; }

            public List<ProductLine> ProductLines { get; set; } = new List<ProductLine>();
        }

        public class ProductLine
        {
            public int ProductLineId { get; set; }
            public int OrderId { get; set; }
            public string ProductCode { get; set; }
            public Order Order { get; set; }
        }

        public class TestBuilder : KeyBuilder
        {
            public TestBuilder()
            {
                AddRelationship<Order2, LinkedClass2>(o => o.Link2, (o, p) => o.OrderId == p.OrderId);
                AddRelationship<Order1, LinkedClass1>(o => o.Link1, (o, p) => o.OrderId == p.OrderId);
                AddRelationship<Order, List<ProductLine>, ProductLine>(o => o.ProductLines, (o, p) => o.OrderId == p.OrderId);
                AddRelationship<ProductLine, Order>(o => o.Order, (p, o) => o.OrderId == p.OrderId);

                AddKey<Order, int>(o => o.OrderId);
                AddKey<ProductLine, int>(o => o.ProductLineId);
                AddKey<LinkedClass1, int>(o => o.Id);
                AddKey<LinkedClass2, int>(o => o.Id);
                AddKey<LinkedClass3, int>(o => o.Id);
            }
        }
    }
}