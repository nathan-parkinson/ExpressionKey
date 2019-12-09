using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionKey;
using System.Collections.ObjectModel;
using ExpressionKey.Comparers;
using System.Linq.Expressions;
using ExpressionKey.Visitors;

namespace ExpressionKeyTests
{
    public class KeyComparerTests
    {
        [Test]
        public void GetTypeToUseTest()
        {
            Expression<Func<Person, int>> p1 = p => p.Id;
            Expression<Func<Person, int>> p2 = p => 23;
            Expression<Func<Person, string>> p3 = p => "fdhtfghty";

            p2 = ParameterReplacer.Replace(p2, p2.Parameters[0], p1.Parameters[0]) as Expression<Func<Person, int>>;
            p3 = ParameterReplacer.Replace(p3, p3.Parameters[0], p1.Parameters[0]) as Expression<Func<Person, string>>;

            //var func = RelationshipComparer<Person, Person>.CreateHashCodeOld<Person>(new List<Expression> { p1, p2, p3 }, p1.Parameters[0]);

           // var func = KeyComparer<Person>.CreateGetHashCodeFunc<Person>(new List<LambdaExpression> { p1, p2, p3 });

        }

        public class Person 
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}