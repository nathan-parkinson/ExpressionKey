using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ExpressionKey
{
    //TODO update this to match the method in Linq2DB.Include (i.e. restore caching)
    public static class ReflectionExtensions
    {
        internal static Action<TElement, TValue> CreatePropertySetter<TElement, TValue>(
            this Type elementType, string propertyName)
        {
            var pi = elementType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            var mi = pi.GetSetMethod();

            var oParam = Expression.Parameter(elementType, "obj");
            var vParam = Expression.Parameter(typeof(TValue), "val");
            var mce = Expression.Call(oParam, mi, vParam);
            var action = Expression.Lambda<Action<TElement, TValue>>(mce, oParam, vParam);

            return action.Compile();
        }
    }
}
