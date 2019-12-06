using System.Linq.Expressions;
using System.Reflection;

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
