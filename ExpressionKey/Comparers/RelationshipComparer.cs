using ExpressionKey.Visitors;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ExpressionKey.Comparers
{
    public class RelationshipComparer<TKey, TValue> : IRelationshipComparer, IEqualityComparer<ExpressionKey<TKey, TValue>>
    {
        public RelationshipComparer(Expression<Func<TKey, TValue, bool>> expression)
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



#if NETSTANDARD2_0
        private static Func<T, int> CreateHashCode<T>(IEnumerable<Expression> keys, ParameterExpression oldParam)
        {
            var expVar = Expression.Variable(typeof(int), "hashCode");
            var assign = Expression.Assign(expVar, Expression.Constant(-984676295));

            var type = typeof(T);

            var exps = new List<Expression> { assign };
            var expConst = Expression.Constant(-1521134295);
            var param = Expression.Parameter(type, "param");

            foreach (var key in keys)
            {
                var ex1 = Expression.MultiplyAssign(expVar, expConst);
                exps.Add(ex1);

                var exprWithNewParam = ParameterReplacer.Replace(key, oldParam, param) as LambdaExpression;

                var getHashCode = Expression.Call(exprWithNewParam.Body, exprWithNewParam.ReturnType.GetMethod(nameof(object.GetHashCode), new Type[0]));

                if (exprWithNewParam.ReturnType.IsClass)
                {
                    var isNotNull = Expression.NotEqual(exprWithNewParam.Body, Expression.Constant(null, exprWithNewParam.ReturnType));
                    exps.Add(Expression.IfThen(isNotNull, Expression.AddAssign(expVar, getHashCode)));
                }
                else
                {
                    exps.Add(Expression.AddAssign(expVar, getHashCode));
                }


            }

            var returnTarget = Expression.Label(typeof(int));
            var returnExpression = Expression.Return(returnTarget, expVar, typeof(int));
            var returnLabel = Expression.Label(returnTarget, Expression.Constant(0));
            exps.Add(returnExpression);
            exps.Add(returnLabel);

            var block = Expression.Block(new List<ParameterExpression> { expVar }, exps);

            var lamdba = Expression.Lambda<Func<T, int>>(block, param);
            var func = lamdba.Compile();

            return func;
        }
#endif

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
