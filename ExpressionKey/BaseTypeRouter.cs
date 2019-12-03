using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionKey
{
    internal static class BaseTypeRouter<T>
    {
        internal static readonly Func<EntityPool, IEnumerable<T>> GetAllEntities = BuildBaseGetEntities();
        internal static readonly Action<EntityPool, IEnumerable<T>> AddEntities = BuildBaseAddEntities();
        internal static readonly Func<EntityPool, IEnumerable<T>, List<T>> GetEntities = BuildBaseConsolidateEntities();

        private static Func<EntityPool, IEnumerable<T>, List<T>> BuildBaseConsolidateEntities()
        {
            var type = typeof(T);
            var baseType = type.GetRealBaseType();

            var argPool = Expression.Parameter(typeof(EntityPool), "entityPool");
            var arg = Expression.Parameter(typeof(IEnumerable<T>), "entities");
            var call = Expression.Call(argPool, nameof(EntityPool.GetEntities), new Type[] { type, baseType }, arg);

            var lambda = Expression.Lambda<Func<EntityPool, IEnumerable<T>, List<T>>>(call, argPool, arg);
            var func = lambda.Compile();
            return func;
        }


        private static Func<EntityPool, IEnumerable<T>> BuildBaseGetEntities()
        {
            var type = typeof(T);
            var baseType = type.GetRealBaseType();

            var argPool = Expression.Parameter(typeof(EntityPool), "entityPool");
            var call = Expression.Call(argPool, nameof(EntityPool.GetAllEntities), new Type[] { type, baseType });

            var lambda = Expression.Lambda<Func<EntityPool, IEnumerable<T>>>(call, argPool);
            var func = lambda.Compile();
            return func;
        }

        private static Action<EntityPool, IEnumerable<T>> BuildBaseAddEntities()
        {
            var type = typeof(T);
            var baseType = type.GetRealBaseType();

            var argPool = Expression.Parameter(typeof(EntityPool), "entityPool");
            var arg = Expression.Parameter(typeof(IEnumerable<T>), "entities");
            var call = Expression.Call(argPool, nameof(EntityPool.AddEntities), new Type[] { type, baseType }, arg);

            var lambda = Expression.Lambda<Action<EntityPool, IEnumerable<T>>>(call, argPool, arg);
            var action = lambda.Compile();
            return action;
        }
    }

}