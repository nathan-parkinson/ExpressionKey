using System.Collections.Generic;
using System.Linq.Expressions;

namespace ExpressionKey.Visitors
{
    public sealed class ParameterExtractor : ExpressionVisitor
    {
        private readonly HashSet<ParameterExpression> _parameters = new HashSet<ParameterExpression>();

        private ParameterExtractor()
        {
        }

        public static HashSet<ParameterExpression> ExtractParameters(Expression expr)
        {
            var l = new ParameterExtractor();
            l.Visit(expr);

            return l._parameters;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            _parameters.Add(node);
            return base.VisitParameter(node);
        }
    }
}
