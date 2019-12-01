using ExpressionKey.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionKey.Comparers
{
    public class KeyComparer<TKey> : IKeyComparer, IEqualityComparer<TKey>
    {
        private readonly Func<TKey, TKey, bool> _keyKeyMatcher;
        private readonly Func<TKey, int> _keyHasherFunc;

        public KeyComparer(IEnumerable<LambdaExpression> expressions)
        {
            var keyExpressions = expressions.ToList();

            _keyKeyMatcher = CreateMatchExpression<TKey>(keyExpressions);
            _keyHasherFunc = CreateHashCode<TKey>(keyExpressions);
        }

        private static Func<T1, T1, bool> CreateMatchExpression<T1>(List<LambdaExpression> expressions)
        {
            Expression buildExpr = null;
            var param1 = Expression.Parameter(typeof(T1), "p1");
            var param2 = Expression.Parameter(typeof(T1), "p2");

            for (int i = 0; i < expressions.Count; i++)
            {
                var left = ParameterReplacer.Replace(expressions[i], expressions[i].Parameters.First(), param1) as LambdaExpression;
                var right = ParameterReplacer.Replace(expressions[i], expressions[i].Parameters.First(), param2) as LambdaExpression;

                var expr = Expression.Equal(left.Body, right.Body);
                if (buildExpr == null)
                {
                    buildExpr = expr;
                }
                else
                {
                    buildExpr = Expression.AndAlso(buildExpr, expr);
                }
            }

            var lambda = Expression.Lambda<Func<T1, T1, bool>>(buildExpr, param1, param2);
            var func = lambda.Compile();
            return func;
        }

        private static Func<T, int> CreateHashCode<T>(IEnumerable<LambdaExpression> keys)
        {
            var param = Expression.Parameter(typeof(T), "source");
            var hasher = Expression.New(typeof(HashCode));
            var hasherVariable = Expression.Variable(typeof(HashCode), "h");
            var assign = Expression.Assign(hasherVariable, hasher);
            var expressions = new List<Expression>
            {
                hasherVariable,
                assign
            };

            foreach (var key in keys)
            {
                var exprWithNewParam = ParameterReplacer.Replace(key, key.Parameters.First(), param) as LambdaExpression;
                
                expressions.Add(Expression.Call(hasherVariable, nameof(HashCode.Add), new Type[] { key.ReturnType }, exprWithNewParam.Body));
            }

            var returnTarget = Expression.Label(typeof(int));
            expressions.Add(Expression.Return(returnTarget,
                Expression.Call(hasherVariable, typeof(HashCode).GetMethod(nameof(HashCode.ToHashCode))), typeof(int)));
            expressions.Add(Expression.Label(returnTarget, Expression.Constant(default(int))));

            var block = Expression.Block(new[] { hasherVariable }, expressions);

            var lambda = Expression.Lambda<Func<T, int>>(block, param);
            return lambda.Compile();
        }

        public bool Equals(TKey x, TKey y) => _keyKeyMatcher(x, y);
        public int GetHashCode(TKey obj) =>  _keyHasherFunc(obj);
    }
}
