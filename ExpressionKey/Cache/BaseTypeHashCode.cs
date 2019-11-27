using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionKey.Cache
{
    class BaseTypeSet<TBase> : IBaseTypeSet
    {
        public BaseTypeSet(IEnumerable<Expression<Func<TBase, object>>> pkExpresssions)
        { 
            Data = new HashSet<TBase>(new KeyComparer<TBase>(pkExpresssions));
        }

        public HashSet<TBase> Data { get; }
    }
}
