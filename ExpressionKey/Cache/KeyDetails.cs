using ExpressionKey.Comparers;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ExpressionKey.Cache
{


    public class KeyDetails
    {
        public KeyDetails(KeyDetails key, LambdaExpression fields)
        {
            Type = key.Type;
            BaseType = key.BaseType;
            Fields.AddRange(key.Fields);
            Fields.Add(fields);
        }

        public KeyDetails(Type type, Type baseType, LambdaExpression fields)
        {
            Type = type;
            BaseType = baseType;
            Fields.Add(fields);
        }

        public KeyDetails(KeyDetails key, IKeyComparer comparer)
        {
            Type = key.Type;
            BaseType = key.BaseType;
            Fields.AddRange(key.Fields);
            KeyComparer = comparer;
        }

        public Type Type { get; }
        public Type BaseType { get; }
        public List<LambdaExpression> Fields { get; } = new List<LambdaExpression>();

        public IKeyComparer KeyComparer { get; }
    }
}
