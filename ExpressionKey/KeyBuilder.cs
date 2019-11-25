using ExpressionKey.Cache;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ExpressionKey
{
    public class KeyBuilder
    {
        internal IEnumerable<Expression<Func<T, object>>> GetPrimaryKeys<T>()
        {
            return Enumerable.Empty<Expression<Func<T, object>>>();
        }

        internal IEnumerable<(MemberInfo member, LambdaExpression expression, LambdaExpression property)> GetForeignKeys<T>()
        {
            var tt = typeof(T);
            return Enumerable.Empty<(MemberInfo, LambdaExpression, LambdaExpression)>();
        }
    }
}
