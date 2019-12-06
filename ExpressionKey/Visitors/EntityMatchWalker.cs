using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionKey.Visitors
{
    public sealed class EntityMatchWalker : ExpressionVisitor
    {
        private bool _ignore;
        private bool _isNot;

        private Expression _expression;

        private readonly List<Expression> _thisKey = new List<Expression>();
        private readonly List<Expression> _otherKey = new List<Expression>();

        private readonly ParameterExpression _thisParam;
        private readonly ParameterExpression _otherParam;

        private EntityMatchWalker(ParameterExpression thisParameter, ParameterExpression otherParameter)
        {
            _thisParam = thisParameter;
            _otherParam = otherParameter;
        }

        public static Tuple<List<Expression>, List<Expression>> ExtractKeyNodes(Expression expression, ParameterExpression thisParameter, ParameterExpression otherParameter)
        {
            var walker = new EntityMatchWalker(thisParameter, otherParameter);
            walker.Visit(expression);
            return Tuple.Create(walker._thisKey, walker._otherKey);
        }

        private void AddKeysToList(BinaryExpression node, HashSet<ParameterExpression> leftParams, HashSet<ParameterExpression> rightParams)
        {
            if (!(_isNot && node.NodeType == ExpressionType.Equal) || (_isNot && node.NodeType == ExpressionType.NotEqual))
            {
                bool? isLeftThisKey = null;
                if(leftParams.Contains(_thisParam))
                {
                    isLeftThisKey = true;
                }

                if (rightParams.Contains(_thisParam))
                {
                    isLeftThisKey = false;
                }

                if (!isLeftThisKey.HasValue)
                {
                    throw new ArgumentException($"At least one part of '{nameof(BinaryExpression)}' must " +
                        "relate to table field");
                }

                if (isLeftThisKey.Value)
                {
                    _thisKey.Add(node.Left);
                    _otherKey.Add(node.Right);
                }
                else
                {
                    _thisKey.Add(node.Right);
                    _otherKey.Add(node.Left);
                }
            }
        }

        private bool AreParametersCorrect(HashSet<ParameterExpression> leftParams, HashSet<ParameterExpression> rightParams)
            //Result must be:
            // * Left = 1 param : Right = none
            // * Left = none param : Right = 1
            // * Left = none param : Right = none
            // * Left = 1 param : Right = 1 (must be the other param)
            => leftParams.Count < 2 && rightParams.Count < 2 &&
                (leftParams.Count != rightParams.Count ||
                leftParams.First() != rightParams.First());

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var ignoreAtStart = _ignore;

            _ignore = _ignore || node.NodeType == ExpressionType.OrElse;

            if (!_ignore)
            {
                var leftParams = ParameterExtractor.ExtractParameters(node.Left);
                var rightParams = ParameterExtractor.ExtractParameters(node.Right);

                if ((
                        (!_isNot && node.NodeType == ExpressionType.Equal) ||
                        (_isNot && node.NodeType == ExpressionType.NotEqual)
                    ) && AreParametersCorrect(leftParams, rightParams))
                {
                    AddKeysToList(node, leftParams, rightParams);
                    _expression = _expression == null ? node : Expression.AndAlso(_expression, node);
                }
            }

            var returnVal = base.VisitBinary(node);

            if (!ignoreAtStart && _ignore)
            {
                _ignore = false;
            }

            return returnVal;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            var isNotAtStart = _isNot;

            _isNot = _isNot || node.NodeType == ExpressionType.Not;

            var returnVal = base.VisitUnary(node);

            if (!isNotAtStart && _isNot)
            {
                _isNot = false;
            }

            return returnVal;
        }
    }
}
