using ExpressionKey.Cache;
using ExpressionKey.Comparers;
using ExpressionKey.Visitors;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

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
