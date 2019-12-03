using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionKey;
using System.Collections.ObjectModel;

namespace ExpressionKeyTests
{
    public class ReflectionExtensionTests
    {
        [TestCase(typeof(IEnumerable<Person>), typeof(Person))]
        [TestCase(typeof(ICollection<Person>), typeof(Person))]
        [TestCase(typeof(IList<Person>), typeof(Person))]
        [TestCase(typeof(Collection<Person>), typeof(Person))]
        [TestCase(typeof(List<Person>), typeof(Person))]
        public void GetTypeToUseTest(Type type1, Type type2)
        {
            var type = type1.GetTypeToUse();
            Assert.AreEqual(type2, type);
        }

        public class Person { }
    }
}