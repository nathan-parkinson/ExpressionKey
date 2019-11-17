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

        // T = Child
        // U = Parent
        // property = Child.Parent
        // JoinExpression = (c, p) => c.ParentId == p.Id
        //TODO find better names for the parameters
        public static IEnumerable<T> SetReferences<T, U>(
            this IEnumerable<T> child,
            Expression<Func<T, U>> property,
            IEnumerable<U> parent,
            Expression<Func<T, U, bool>> joinExpression)
        {
            var member = MemberExtractor.ExtractSingleMember(property);
            var setter = typeof(T).CreatePropertySetter<T, U>(member.Member.Name);

            var collection = child as ICollection<T> ?? child.ToList();
            var lookup = collection.ToExpressionKeyLookup(joinExpression);

            if (lookup != null)
            {
                foreach (var parentItem in parent)
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
                foreach (var childItem in child)
                {
                    var matchingParent = parent.FirstOrDefault(x => func(childItem, x));
                    setter(childItem, matchingParent);
                }
            }
            return collection;
        }


        public static void SetReferences<T, U>(
            this IEnumerable<T> pool1,
            Expression<Func<T, IEnumerable<U>>> property,
            ICollection<U> pool2,
            Expression<Func<T, U, bool>> joinExpression)
        {

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