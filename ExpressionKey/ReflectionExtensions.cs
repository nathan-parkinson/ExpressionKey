﻿using ExpressionKey.Cache;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ExpressionKeyTests")]
namespace ExpressionKey
{
    public static class ReflectionExtensions
    {
        readonly static ConcurrentDictionary<PropertySetterKey, IPropertySetterCache> _propertySetterCache =
            new ConcurrentDictionary<PropertySetterKey, IPropertySetterCache>();

        readonly static ConcurrentDictionary<PropertySetterKey, IPropertySetterCache> _collectionSetterCache =
            new ConcurrentDictionary<PropertySetterKey, IPropertySetterCache>();

        readonly static ConcurrentDictionary<PropertySetterKey, ICollectionInitializer> _collectionInitialiserCache =
            new ConcurrentDictionary<PropertySetterKey, ICollectionInitializer>();

        internal static Type GetTypeToUse(this Type type)
        {
            if (type.IsGenericType)
            {
                if (type.IsInterface && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return type.GetGenericArguments()[0];
                }

                var genericTypeDefinition = type.GetGenericTypeDefinition();

                if (genericTypeDefinition.GetInterfaces()
                            .Any(t => t.IsGenericType &&
                                      t.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return type;
        }

        internal static bool IsIEnumerable(this Type type)
        {
            if (type.IsGenericType)
            {
                if (type.IsInterface)
                {
                    var def = type.GetGenericTypeDefinition();

                    return def == typeof(IEnumerable<>) || type.GetGenericTypeDefinition().GetInterfaces()
                                .Any(x => x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                }

                var genericTypeDefinition = type.GetGenericTypeDefinition();

                return genericTypeDefinition.GetInterfaces()
                            .Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            }

            return false;
        }

        internal static Action<TElement, TValue> CreatePropertySetter<TElement, TValue>(
            this Type elementType, string propertyName)
        {
            var key = new PropertySetterKey(elementType, typeof(TValue), propertyName);
            var setter = _propertySetterCache.GetOrAdd(key, k =>
            {
                var pi = k.ElementType.GetProperty(k.PropertyName, BindingFlags.Public | BindingFlags.Instance);
                var mi = pi.GetSetMethod();

                var oParam = Expression.Parameter(k.ElementType, "obj");
                var vParam = Expression.Parameter(k.ValueType, "val");
                var mce = Expression.Call(oParam, mi, vParam);
                var action = Expression.Lambda<Action<TElement, TValue>>(mce, oParam, vParam);

                return new PropertySetterCache<TElement, TValue>(action.Compile());
            });

            var typedSetter = setter as PropertySetterCache<TElement, TValue>;
            return typedSetter?.Setter;
        }

        internal static Action<TElement, TValue> CreateCollectionPropertySetter<TElement, TValue>(
                this Type elementType, string propertyName, Type propertyType)
        {
            var key = new PropertySetterKey(elementType, typeof(TValue), propertyName);

            var setter = _collectionSetterCache.GetOrAdd(key, k =>
            {
                var oParam = Expression.Parameter(k.ElementType, "obj");
                var vParam = Expression.Parameter(k.ValueType, "val");
                var mce = Expression.Call(
                                Expression.Convert(
                                    Expression.Property(oParam, k.PropertyName)
                                , typeof(ICollection<TValue>))
                          , typeof(ICollection<TValue>).GetMethod("Add"), vParam);

                var action = Expression.Lambda<Action<TElement, TValue>>(mce, oParam, vParam);

                return new PropertySetterCache<TElement, TValue>(action.Compile());
            });

            var typedSetter = setter as PropertySetterCache<TElement, TValue>;
            return typedSetter?.Setter;
        }



        internal static Func<TParent, IEnumerable<TChild>> CreatePropertySetup<TParent, TChild>(this Type itemType, string propertyName)
            //where TParent : class
            //where TChild : class
        {
            var key = new PropertySetterKey(itemType, typeof(TChild), propertyName);

            var setter = _collectionInitialiserCache.GetOrAdd(key, k =>
            {

                var pi = itemType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

                var tChildType = pi.PropertyType;
                var parentParam = Expression.Parameter(itemType, "p");
                var property = Expression.Property(parentParam, propertyName);

                var isParamNull = Expression.Equal(property, Expression.Constant(null));

                var collectionTypeToCreate = GetTypeToCreate(tChildType);
                if (collectionTypeToCreate.GetConstructor(Type.EmptyTypes) == null)
                {
                    throw new ArgumentException("Collection must have a parameterless constructor. Try instantiating " +
                        "the item in the owners ctor");
                }

                Expression newCollection = null;
                if (collectionTypeToCreate.IsGenericTypeDefinition)
                {
                    newCollection = Expression.New(collectionTypeToCreate.MakeGenericType(new Type[] { typeof(TChild) }));
                }
                else
                {
                    newCollection = Expression.New(collectionTypeToCreate);
                }

                var mi = pi.GetSetMethod();
                var mce = Expression.Call(parentParam, mi, newCollection);

                var @if = Expression.IfThen(isParamNull, mce);

                var expressions = new List<Expression>
                {
                    @if
                };

                var returnTarget = Expression.Label(typeof(IEnumerable<TChild>));
                expressions.Add(Expression.Return(returnTarget, property, typeof(IEnumerable<TChild>)));
                expressions.Add(Expression.Label(returnTarget, Expression.Constant(default(IEnumerable<TChild>), typeof(IEnumerable<TChild>))));

                var block = Expression.Block(expressions);



                var finalCode = Expression.Lambda<Func<TParent, IEnumerable<TChild>>>(block, parentParam);

                return new CollectionInitializer<TParent, TChild>(finalCode.Compile());
            });

            var typesSetter = setter as CollectionInitializer<TParent, TChild>;

            return typesSetter?.Initializer;
        }

        private static Type GetTypeToCreate(Type type)
        {
            if (type.IsClass && !type.IsAbstract)
            {
                return type;
            }

            int typeNum = 3;
            var def = type.GetGenericTypeDefinition();

            if (type.IsInterface)
            {

                if (def == typeof(ISet<>))
                {
                    typeNum = 1;
                }
                else if (def == typeof(IList<>))
                {
                    typeNum = 2;
                }
                else if (def == typeof(ICollection<>) || def == typeof(IEnumerable<>))
                {
                    typeNum = 3;
                }
                else
                {
                    typeNum = def.GetInterfaces()
                                .Where(x => x.IsGenericType)
                                .Min(x => x.GetGenericTypeDefinition() == typeof(ISet<>) ? 1 :
                                            x.GetGenericTypeDefinition() == typeof(IList<>) ? 2 : 3);
                }
            }
            else
            {
                typeNum = def.GetInterfaces()
                           .Where(t => t.IsGenericType)
                           .Min(x => x.GetGenericTypeDefinition() == typeof(ISet<>) ? 1 :
                                            x.GetGenericTypeDefinition() == typeof(IList<>) ? 2 : 3);
            }

            switch (typeNum)
            {
                case 1:
                    return typeof(HashSet<>);
                case 2:
                    return typeof(List<>);
                default:
                    return typeof(Collection<>);
            }
        }

        internal static Type GetRealBaseType(this Type type)
        {
            var baseType = type.BaseType;
            var objType = typeof(object);

            while (baseType != objType && baseType != typeof(ValueType) && !type.IsInterface && !baseType.IsInterface)
            {
                type = baseType;
                baseType = type.BaseType;
            }

            return type;
        }


        internal static IEnumerable<Type> GetAllBaseTypes(this Type type)
        {
            var types = new List<Type> { type };
            var baseType = type.BaseType;
            var objType = typeof(object);

            while (baseType != objType && baseType != typeof(ValueType) && !type.IsInterface && !baseType.IsInterface)
            {
                type = baseType;
                types.Add(type);
                baseType = type.BaseType;
            }

            return types;
        }

        internal static bool IsNullable(this Type type) => (type.IsClass || Nullable.GetUnderlyingType(type) != null);
    }
}
