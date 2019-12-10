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
                var memberExpr = MemberExtractor.ExtractSingleMember(exprWithNewParam.Body);
                var memberType = memberExpr.Member.GetMemberUnderlyingType();

                var hashCodeAdd = Expression.Call(hasherVariable, nameof(HashCode.Add), new Type[] { key.ReturnType }, exprWithNewParam.Body);
                                  
                if (memberType.IsNullable())
                {
                    var isNull = Expression.NotEqual(memberExpr, Expression.Constant(null, memberType));
                    var @if = Expression.IfThen(isNull, hashCodeAdd);
                    expressions.Add(@if);
                }
                else
                {
                    expressions.Add(hashCodeAdd);
                }
            }

            var returnTarget = Expression.Label(typeof(int));
            expressions.Add(Expression.Return(returnTarget,
                Expression.Call(hasherVariable, typeof(HashCode).GetMethod(nameof(HashCode.ToHashCode))), typeof(int)));
            expressions.Add(Expression.Label(returnTarget, Expression.Constant(default(int))));

            var block = Expression.Block(new[] { hasherVariable }, expressions);

            var lambda = Expression.Lambda<Func<T, int>>(block, param);
            return lambda.Compile();
        }

/*
        private static Func<T, int> CreateHashCode<T>(IEnumerable<LambdaExpression> keys)
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

                var exprWithNewParam = ParameterReplacer.Replace(key, key.Parameters.First(), param) as LambdaExpression;
                
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
*/

        public bool Equals(TKey x, TKey y) => _keyKeyMatcher(x, y);
        public int GetHashCode(TKey obj) => _keyHasherFunc(obj);
    }
}
