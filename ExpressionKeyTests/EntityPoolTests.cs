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
            Dictionary<Type, List<ForeignKey>> _fkDict = new Dictionary<Type, List<ForeignKey>>();
            Dictionary<Type, List<LambdaExpression>> _pkDict = new Dictionary<Type, List<LambdaExpression>>();

            private Expression<Func<T, U>> Member<T, U>(Expression<Func<T, U>> expr) => expr;
            private Expression<Func<T, U, bool>> Join<T, U>(Expression<Func<T, U, bool>> expr) => expr;

            public Builder()
            {
                _fkDict.Add(typeof(Order), new List<ForeignKey>{
                    new ForeignKey
                {
                    Member = typeof(Order).GetMember(nameof(Order.ProductLines)).First(),
                    Property = Member<Order, List<ProductLine>>(o => o.ProductLines),
                    Expression = Join<Order, ProductLine>((o, p) => o.OrderId == p.OrderId)
                }});

                _fkDict.Add(typeof(ProductLine), new List<ForeignKey>{
                    new ForeignKey
                {
                    Member = typeof(ProductLine).GetMember(nameof(ProductLine.Order)).First(),
                    Property = Member<ProductLine, Order>(o => o.Order),
                    Expression = Join<ProductLine, Order>((o, p) => o.OrderId == p.OrderId)
                }});


                _pkDict.Add(typeof(Order), new List<LambdaExpression>
                {
                   Member<Order, int>(o => o.OrderId)
                });


                _pkDict.Add(typeof(ProductLine), new List<LambdaExpression>
                {
                    Member<ProductLine, int>(o => o.ProductLineId)
                });
            }

            public override IEnumerable<ForeignKey> GetForeignKeys<T>() => _fkDict[typeof(T)];

            public override IEnumerable<LambdaExpression> GetPrimaryKeys<T>() => _pkDict[typeof(T)];
        }
    }
}