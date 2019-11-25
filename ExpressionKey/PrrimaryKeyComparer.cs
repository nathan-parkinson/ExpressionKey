using ExpressionKey.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionKey
{
    public class PrimaryKeyComparer<TKey> : IEqualityComparer<TKey>
    {
        public PrimaryKeyComparer(IEnumerable<Expression<Func<TKey, object>>> expressions)
        {
            var parameter = Expression.Parameter(typeof(TKey));
            var keyExpressions = expressions.Cast<Expression>().ToList();

            KeyKeyMatcher = CreateMatchExpression<TKey, TKey>(parameter, keyExpressions, keyExpressions);
            KeyHasherFunc = CreateHashCode<TKey>(keyExpressions, parameter);
        }

        private static Func<T1, T2, bool> CreateMatchExpression<T1, T2>(ParameterExpression oldParam, List<Expression> leftExpressions, List<Expression> rightExpressions)
        {
            Expression buildExpr = null;
            var param1 = Expression.Parameter(typeof(T1));
            var param2 = Expression.Parameter(typeof(T2));

            for (int i = 0; i < leftExpressions.Count; i++)
            {
                var left = ParameterReplacer.Replace(leftExpressions[i], oldParam, param1);
                var right = ParameterReplacer.Replace(rightExpressions[i], oldParam, param2);

                var expr = Expression.Equal(left, right);
                if (buildExpr == null)
                {
                    buildExpr = expr;
                }
                else
                {
                    buildExpr = Expression.AndAlso(buildExpr, expr);
                }
            }

            var lambda = Expression.Lambda<Func<T1, T2, bool>>(buildExpr, param1, param2);
            var func = lambda.Compile();
            return func;
        }

        private static Func<T, int> CreateHashCode<T>(IEnumerable<Expression> keys, ParameterExpression oldParam)
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
                var exprWithNewParam = ParameterReplacer.Replace(key, oldParam, param);

                expressions.Add(Expression.Call(hasherVariable, nameof(HashCode.Add), new Type[] { key.Type }, exprWithNewParam));
            }

            var returnTarget = Expression.Label(typeof(int));
            expressions.Add(Expression.Return(returnTarget,
                Expression.Call(hasherVariable, typeof(HashCode).GetMethod(nameof(HashCode.ToHashCode))), typeof(int)));
            expressions.Add(Expression.Label(returnTarget, Expression.Constant(default(int))));

            var block = Expression.Block(new[] { hasherVariable }, expressions);

            var lambda = Expression.Lambda<Func<T, int>>(block, param);
            return lambda.Compile();
        }

        public Func<TKey, TKey, bool> KeyKeyMatcher { get; set; }
        public Func<TKey, int> KeyHasherFunc { get; }
        public bool Equals(TKey x, TKey y) => KeyKeyMatcher(x, y);
        public int GetHashCode(TKey obj) => KeyHasherFunc(obj);
    }
}
