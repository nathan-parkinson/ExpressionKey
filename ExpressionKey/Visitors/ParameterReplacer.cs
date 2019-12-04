using System.Linq.Expressions;

namespace ExpressionKey.Visitors
{
    public sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _old;
        private readonly ParameterExpression _new;

        private ParameterReplacer(ParameterExpression old, ParameterExpression @new)
        {
            _old = old;
            _new = @new;
        }

        public static Expression Replace(Expression expr, ParameterExpression old, ParameterExpression @new)
            => new ParameterReplacer(old, @new).Visit(expr);

        protected override Expression VisitParameter(ParameterExpression node)
        {
            var local = node;
            if(local == _old)
            {
                local = _new;
            }

            return base.VisitParameter(local);
        }
    }
}
