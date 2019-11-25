using ExpressionKey.Cache;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ExpressionKey
{
    //Make this an abstract type to be implememented depending on the data source
    public abstract class KeyBuilder
    {
        public abstract IEnumerable<LambdaExpression> GetPrimaryKeys<T>();
        public abstract IEnumerable<ForeignKey> GetForeignKeys<T>();
    }

    public class ForeignKey
    {
        public MemberInfo Member { get; set; }
        public LambdaExpression Expression { get; set; }
        public LambdaExpression Property { get; set; }
    }
        
}
