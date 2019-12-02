using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionKey.Visitors
{
    public class MemberExtractor : ExpressionVisitor
    {
        private readonly HashSet<MemberExpression> _members = new HashSet<MemberExpression>();
        public static MemberExpression ExtractSingleMember(Expression expr)
        {
            var visitor = new MemberExtractor();
            visitor.Visit(expr);

            if(visitor._members.Count == 0)
            {
                throw new ArgumentNullException(nameof(expr), "No MemberExpression nodes found");
            }

            if(visitor._members.Count > 1)
            {
                throw new InvalidOperationException("More than 1 MemberExpression nodes found");
            }

            return visitor._members.First();
        }

        public static IEnumerable<MemberExpression> ExtractMembers(Expression expr)
        {
            var visitor = new MemberExtractor();
            visitor.Visit(expr);

            return visitor._members;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _members.Add(node);
            return base.VisitMember(node);
        }
    }
}
