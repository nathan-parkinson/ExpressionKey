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


            var pool = new EntityPool(new Builder());
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


            var pool = new EntityPool(new Builder());
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


            var pool = new EntityPool(new Builder());
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


            var pool = new EntityPool(new Builder());
            pool.AddEntities(people);
            pool.AddEntities(children);
            pool.AddEntities(links1);
            pool.AddEntities(links2);

            Assert.IsTrue(people.All(x => x.ProductLines.All(c => c.OrderId == x.OrderId)));
            Assert.IsTrue(children.All(c => c.Order.OrderId == c.OrderId));
            Assert.IsTrue(people.All(c => c.Link1.OrderId == c.OrderId));
            Assert.IsTrue(people.All(c => c.Link2.OrderId == c.OrderId));
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

        public class Builder : KeyBuilder
        {
            private Expression<Func<T, U>> Member<T, U>(Expression<Func<T, U>> expr) => expr;
            private Expression<Func<T, U, bool>> Join<T, U>(Expression<Func<T, U, bool>> expr) => expr;

            public Builder()
            {

                ForeignKeyDict.Add(typeof(Order2), new List<ForeignKey>{
                    new ForeignKey
                {
                    Member = typeof(Order2).GetMember(nameof(Order2.Link2)).First(),
                    Property = Member<Order2, LinkedClass2>(o => o.Link2),
                    Expression = Join<Order2, LinkedClass2>((o, p) => o.OrderId == p.OrderId)
                }});


                ForeignKeyDict.Add(typeof(Order1), new List<ForeignKey>{
                    new ForeignKey
                {
                    Member = typeof(Order1).GetMember(nameof(Order1.Link1)).First(),
                    Property = Member<Order1, LinkedClass1>(o => o.Link1),
                    Expression = Join<Order1, LinkedClass1>((o, p) => o.OrderId == p.OrderId)
                }});


                ForeignKeyDict.Add(typeof(Order), new List<ForeignKey>{
                    new ForeignKey
                {
                    Member = typeof(Order).GetMember(nameof(Order.ProductLines)).First(),
                    Property = Member<Order, List<ProductLine>>(o => o.ProductLines),
                    Expression = Join<Order, ProductLine>((o, p) => o.OrderId == p.OrderId)
                }});

                ForeignKeyDict.Add(typeof(ProductLine), new List<ForeignKey>{
                    new ForeignKey
                {
                    Member = typeof(ProductLine).GetMember(nameof(ProductLine.Order)).First(),
                    Property = Member<ProductLine, Order>(o => o.Order),
                    Expression = Join<ProductLine, Order>((o, p) => o.OrderId == p.OrderId)
                }});





                PrimaryKeyDict.Add(typeof(Order), new List<LambdaExpression>
                {
                   Member<Order, int>(o => o.OrderId)
                });


                PrimaryKeyDict.Add(typeof(ProductLine), new List<LambdaExpression>
                {
                    Member<ProductLine, int>(o => o.ProductLineId)
                });

                PrimaryKeyDict.Add(typeof(LinkedClass1), new List<LambdaExpression>
                {
                    Member<LinkedClass1, int>(o => o.Id)
                });


                PrimaryKeyDict.Add(typeof(LinkedClass2), new List<LambdaExpression>
                {
                    Member<LinkedClass2, int>(o => o.Id)
                });


                PrimaryKeyDict.Add(typeof(LinkedClass3), new List<LambdaExpression>
                {
                    Member<LinkedClass3, int>(o => o.Id)
                });

            }
        }
    }
}