using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionKey;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;

namespace ExpressionKeyTests
{
    public class EntityPoolConsolidationTests
    {
        [Test]
        public void EntityPoolConsolidationTest()
        {
            const int count = 100;
            var list1 = Enumerable.Range(1, count).Select(z => new Order
            {
                OrderId = z,
                OrderDate = DateTime.Today.AddDays(z)
            }).ToList();

            var list2 = Enumerable.Range(1, count).Select(z => new Order1
            {
                OrderId = z,
                OrderDate = DateTime.Today.AddDays(z)
            }).ToList();

            var list3 = Enumerable.Range(1, count).Select(z => new Order2
            {
                OrderId = z,
                OrderDate = DateTime.Today.AddDays(z)
            }).ToList();

            var union = list1.Union(list2).Union(list3).ToList();

            var pool = new TestBuilder().CreateEntityPool();
            var unique = pool.ConsolidateEntities(union);

            Assert.AreEqual(unique.Count, count * 3);
            Assert.AreEqual(unique.Distinct().Count(), count);
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