using ExpressionKey.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ExpressionKey
{
    public class ForeignKeyComparer<TKey, TValue> : IEqualityComparer<ExpressionKey<TKey, TValue>>
    {
        public ForeignKeyComparer(Expression<Func<TKey, TValue, bool>> expression)
        {
            var keyParam = expression.Parameters[0];
            var valueParam = expression.Parameters[1];

            var results = EntityMatchWalker.ExtractKeyNodes(expression, keyParam, valueParam);
            var keyExpressions = results.Item1;
            var valueExpressions = results.Item2;

            if(results.Item1.Count == 0 || results.Item2.Count == 0)
            {
                IsExpressionInvalid = true;
                return;
            }

            KeyKeyMatcher = CreateMatchExpression<TKey, TKey>(keyParam, keyExpressions, keyExpressions);
            KeyValueMatcher = expression.Compile();

            KeyHasherFunc = CreateHashCode<TKey>(keyExpressions, keyParam);
            ValueHasherFunc = CreateHashCode<TValue>(valueExpressions, valueParam);
        }

        public bool IsExpressionInvalid { get; }

        private static Func<T1, T2, bool> CreateMatchExpression<T1, T2>(ParameterExpression oldParam, List<Expression> leftExpressions, List<Expression> rightExpressions)
        {
            Expression buildExpr = null;
            var param1 = Expression.Parameter(typeof(T1), "p1");
            var param2 = Expression.Parameter(typeof(T2), "p2");

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

            var lambda = Expression.Lambda<Func<T,  int>>(block, param);
            return lambda.Compile();
        }

        public Func<TKey, TKey, bool> KeyKeyMatcher { get; set; }
        public Func<TKey, TValue, bool> KeyValueMatcher { get; set; }

        public Func<TKey, int> KeyHasherFunc { get;  }
        public Func<TValue, int> ValueHasherFunc { get;  }

        public bool Equals(ExpressionKey<TKey, TValue> x, ExpressionKey<TKey, TValue> y)
        {
            if (x.IsKey && y.IsKey)
            {
                return KeyKeyMatcher(x.KeyItem, y.KeyItem);
            }

            if (!x.IsKey && !y.IsKey)
            {
                throw new ArgumentException("Cannot compare value without key");
            }

            if (x.IsKey)
            {
                return KeyValueMatcher(x.KeyItem, y.ValueItem);
            }

            return KeyValueMatcher(y.KeyItem, x.ValueItem);
        }

        public int GetHashCode(ExpressionKey<TKey, TValue> obj)
        {
            if (obj.IsKey)
            {
                return KeyHasherFunc(obj.KeyItem);
            }

            return ValueHasherFunc(obj.ValueItem);
        }
    }
}
