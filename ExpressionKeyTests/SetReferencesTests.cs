using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionKey;

namespace ExpressionKeyTests
{
    public class EntitySetReferencesTests
    {
        [Test]
        public void TwoWayMappingTest()
        {
            var people = Enumerable.Range(1, 1000).Select(z => new Person
            {
                Name = "Person " + z,
                PersonId = z
            }).ToList();

            var children = Enumerable.Range(1, 10000).Select(z => new Child
            {
                ParentId = (int)Math.Ceiling(z / 10.0f),
                Name = "Child " + z,
                ChildId = z
            }).ToList();


            people.SetReferences(p => p.Children, children, (p, c) => p.PersonId == c.ParentId, new Builder());
            children.SetReferences(c => c.Parent, people, (c, p) => c.ParentId == p.PersonId);

            Assert.IsTrue(people.All(x => x.Children.All(c => c.ParentId == x.PersonId)));
            Assert.IsTrue(children.All(c => c.Parent.PersonId == c.ParentId));
        }

        [Test]
        public void OneToManyMappingTest()
        {
            var people = Enumerable.Range(1, 1000).Select(z => new Person
            {
                Name = "Person " + z,
                PersonId = z
            }).ToList();

            var children = Enumerable.Range(1, 10000).Select(z => new Child
            {
                ParentId = (int)Math.Ceiling(z / 10.0f),
                Name = "Child " + z,
                ChildId = z
            }).ToList();

            people.SetReferences(p => p.Children, children, (p, c) => p.PersonId == c.ParentId, new Builder());
            Assert.IsTrue(people.All(x => x.Children.All(c => c.ParentId == x.PersonId)));
        }

        [Test]
        public void ManyToOneMappingTest()
        {
            var people = Enumerable.Range(1, 1000).Select(z => new Person
            {
                Name = "Person " + z,
                PersonId = z
            }).ToList();

            var children = Enumerable.Range(1, 10000).Select(z => new Child
            {
                ParentId = (int)Math.Ceiling(z / 10.0f),
                Name = "Child " + z,
                ChildId = z
            }).ToList();

            children.SetReferences(c => c.Parent, people, (c, p) => c.ParentId == p.PersonId);
            Assert.IsTrue(children.All(c => c.Parent.PersonId == c.ParentId));
        }



        [Test]
        public void ManyToOneMappingTest2()
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

            children.SetReferences(c => c.Order, people, (c, p) => c.OrderId == p.OrderId);
            people.SetReferences(o => o.ProductLines, children, (o, p) => p.OrderId == o.OrderId, new Builder());

            Assert.IsTrue(children.All(c => c.Order.OrderId == c.OrderId));
        }

        public class Builder : KeyBuilder
        {
            public Builder()
            {
                AddKey<Order, int>(o => o.OrderId);
                AddKey<ProductLine, int>(p => p.ProductLineId);
                AddKey<Person, int>(p => p.PersonId);
                AddKey<Child, int>(p => p.ChildId);
            }

        }

        public class Person
        {
            public int PersonId { get; set; }
            public int? NestedParentId { get; set; }
            public Person NestedPerson { get; set; }
            public string Name { get; set; }

            public IEnumerable<Child> Children { get; set; }// = new List<Child>();
        }

        public class Child
        {
            public int ChildId { get; set; }
            public int ParentId { get; set; }
            public Person Parent { get; set; }
            public string Name { get; set; }
        }


        public class Order
        {
            public int OrderId { get; set; }
            public DateTime OrderDate { get; set; }

            public IEnumerable<ProductLine> ProductLines { get; set; } = new List<ProductLine>();
        }

        public class ProductLine
        {
            public int ProductLineId { get; set; }
            public int OrderId { get; set; }
            public string ProductCode { get; set; }
            public Order Order { get; set; }
        }

    }
}