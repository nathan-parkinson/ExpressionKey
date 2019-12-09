using ExpressionKey.Comparers;
using ExpressionKey.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpressionKey
{
    public static class Extensions
    {
        public static IExpressionKeyLookup<T, U> ToExpressionKeyLookup<T, U>(
            this IEnumerable<T> items,
            Expression<Func<T, U, bool>> expr)
        {
            var lookup = new ExpressionKeyLookup<T, U>(items, expr);
            if (lookup.IsExpressionInvalid)
            {
                return null;
            }

            return lookup;
        }

        public static IEnumerable<T> SetReferences<T, U>(
            this IEnumerable<T> source,
            Expression<Func<T, U>> property,
            IEnumerable<U> target,
            Expression<Func<T, U, bool>> joinExpression)
        {
            if (source == null || target == null)
            {
                return source;
            }

            var member = MemberExtractor.ExtractSingleMember(property);
            var setter = property.Parameters[0].Type.CreatePropertySetter<T, U>(member.Member.Name);

            var collection = source as ICollection<T> ?? source.ToList();
            var lookup = collection.ToExpressionKeyLookup(joinExpression);

            if (lookup != null)
            {
                foreach (var parentItem in target)
                {
                    foreach (var matchingChild in lookup.GetMatches(parentItem))
                    {
                        //set item to match.property
                        setter(matchingChild, parentItem);
                    }
                }
            }
            //fallback to looping through when no hashCode key can be created
            else
            {
                var func = joinExpression.Compile();
                foreach (var childItem in source)
                {
                    var matchingParent = target.FirstOrDefault(x => func(childItem, x));
                    setter(childItem, matchingParent);
                }
            }
            return collection;
        }


        public static IEnumerable<T> SetReferences<T, V, U>(
        this IEnumerable<T> source,
        Expression<Func<T, V>> property,
        IEnumerable<U> target,
        Expression<Func<T, U, bool>> joinExpression,
        KeyBuilder keyBuilder) where V : IEnumerable<U>
        {
            if (source == null || target == null)
            {
                return source;
            }

            var member = MemberExtractor.ExtractSingleMember(property);
            var setter = typeof(T).CreateCollectionPropertySetter<T, U>(member.Member.Name, member.Type);

            var collection = source as ICollection<T> ?? source.ToList();

            //TODO see if this can be cached?
            Expression<Func<U, T, bool>> reverseJoin = Expression.Lambda<Func<U, T, bool>>(joinExpression.Body, joinExpression.Parameters[1], joinExpression.Parameters[0]);
            var targetLookup = target.ToExpressionKeyLookup(reverseJoin);

            var ifnullSetter = typeof(T).CreatePropertySetup<T, U>(member.Member.Name);
            if (targetLookup != null)
            {
                foreach (var parentEntity in collection)
                {
                    var existingChildEntities = ifnullSetter(parentEntity);

                    var childEntities = new HashSet<U>(
                        targetLookup.GetMatches(parentEntity),
                        keyBuilder.GetKeyComparer<U>());

                    childEntities.ExceptWith(existingChildEntities);
                    foreach (var childEntity in childEntities)
                    {
                        setter(parentEntity, childEntity);
                    }
                }

                return collection;
            }

            var func = joinExpression.Compile();
            foreach (var childItem in collection)
            {
                ifnullSetter(childItem);

                foreach (var matchingParent in target.Where(x => func(childItem, x)))
                {
                    setter(childItem, matchingParent);
                }
            }


            return collection;
        }

        internal static Type GetMemberUnderlyingType(this MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;
                default:
                    throw new ArgumentException("MemberInfo must be if type FieldInfo, PropertyInfo or EventInfo", nameof(member));
            }
        }
    }
}