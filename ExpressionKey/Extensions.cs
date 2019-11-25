using ExpressionKey.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

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

        // T = ProductLines
        // U = Order
        // property = ProductLine.Order
        // JoinExpression = (c, p) => c.OrderId == p.OrderId
        //TODO find better names for the parameters
        public static IEnumerable<T> SetReferences<T, U>(
            this IEnumerable<T> productLines,
            Expression<Func<T, U>> property,
            IEnumerable<U> orders,
            Expression<Func<T, U, bool>> joinExpression)
        {
            var member = MemberExtractor.ExtractSingleMember(property);
            var setter = property.Parameters[0].Type.CreatePropertySetter<T, U>(member.Member.Name);

            var collection = productLines as ICollection<T> ?? productLines.ToList();
            var lookup = collection.ToExpressionKeyLookup(joinExpression);

            if (lookup != null)
            {
                foreach (var parentItem in orders)
                {
                    //TODO look into whether below should be lookup.GetMatches(parentItem).Single()
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
                foreach (var childItem in productLines)
                {
                    var matchingParent = orders.FirstOrDefault(x => func(childItem, x));
                    setter(childItem, matchingParent);
                }
            }
            return collection;
        }


        public static IEnumerable<T> SetReferences<T, V, U>(
            this IEnumerable<T> child,
            Expression<Func<T, V>> property,
            IEnumerable<U> parent,
            Expression<Func<T, U, bool>> joinExpression) where V : IEnumerable<U>
        {
            var member = MemberExtractor.ExtractSingleMember(property);
            var setter = typeof(T).CreateCollectionPropertySetter<T, U>(member.Member.Name, member.Type);

            var collection = child as ICollection<T> ?? child.ToList();
            var lookup = collection.ToExpressionKeyLookup(joinExpression);

            var ifnullSetter = typeof(T).CreatePropertySetup<T, U>(member.Member.Name);

            if (lookup != null)
            {
                foreach (var parentItem in parent)
                {
                    foreach (var matchingChild in lookup.GetMatches(parentItem))
                    {
                        ifnullSetter(matchingChild);
                        //set item to match.property
                        setter(matchingChild, parentItem);
                    }
                }
            }
            //fallback to looping through when no hashCode key can be created
            else
            {
                var func = joinExpression.Compile();
                foreach (var childItem in child)
                {
                    ifnullSetter(childItem);

                    var matchingParent = parent.FirstOrDefault(x => func(childItem, x));
                    setter(childItem, matchingParent);
                }
            }
            return collection;
        }


        /// <summary>
        /// Gets the member's underlying type.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>The underlying type of the member.</returns>
        public static Type GetMemberUnderlyingType(this MemberInfo member)
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