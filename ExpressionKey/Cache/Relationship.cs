using ExpressionKey.Cache;
using ExpressionKey.Comparers;
using ExpressionKey.Visitors;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ExpressionKey.Cache
{
    public class Relationship
    {
        public Relationship(MemberInfo member, LambdaExpression property, LambdaExpression expression)
        {
            Member = member;
            Property = property;
            Expression = expression;
        }

        public MemberInfo Member { get; }
        public LambdaExpression Expression { get; }
        public LambdaExpression Property { get; }
    }
}
