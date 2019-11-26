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
        protected readonly Dictionary<Type, List<ForeignKey>> ForeignKeyDict
            = new Dictionary<Type, List<ForeignKey>>();

        protected readonly Dictionary<Type, List<LambdaExpression>> PrimaryKeyDict
            = new Dictionary<Type, List<LambdaExpression>>();

        public virtual IEnumerable<ForeignKey> GetForeignKeys<T>() 
            => ForeignKeyDict.GetValueOrDefault(typeof(T)) ?? Enumerable.Empty<ForeignKey>();

        public virtual IEnumerable<LambdaExpression> GetPrimaryKeys<T>() 
            => PrimaryKeyDict.GetValueOrDefault(typeof(T)) ?? Enumerable.Empty<LambdaExpression>();
    }

    public class ForeignKey
    {
        public MemberInfo Member { get; set; }
        public LambdaExpression Expression { get; set; }
        public LambdaExpression Property { get; set; }
    }
        
}
