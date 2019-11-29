using ExpressionKey.Cache;
using ExpressionKey.Visitors;
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
        internal readonly ConcurrentDictionary<Type, ITypeHelper> TypeHelpers =
            new ConcurrentDictionary<Type, ITypeHelper>();

        private readonly ConcurrentDictionary<Type, HashSet<Type>> _typeHierarchy =
            new ConcurrentDictionary<Type, HashSet<Type>>();

        //TODO look into making this ConcurrentDictionary
        protected readonly Dictionary<Type, TypeRelationship> RelationshipsDict
            = new Dictionary<Type, TypeRelationship>();

        //TODO look into making this ConcurrentDictionary
        protected readonly Dictionary<Type, Key> KeysDict
            = new Dictionary<Type, Key>();

        public IEntityPool CreateEntityPool() => new EntityPool(this);

        protected void AddRelationship<TEntity, TOther>(Expression<Func<TEntity, TOther>> memberExpr,
            Expression<Func<TEntity, TOther, bool>> relationshipExpr)
        {
            var store = RelationshipsDict.GetValueOrDefault(typeof(TEntity));
            store ??= new TypeRelationship
            {
                Type = typeof(TEntity),
                BaseType = typeof(TEntity).GetRealBaseType()
            };

            store.Relationships.Add(new Relationship
            {
                Member = MemberExtractor.ExtractSingleMember(memberExpr).Member,
                Property = memberExpr,
                Expression = relationshipExpr
            });

            RelationshipsDict[typeof(TEntity)] = store;
            UpdateTypeHierachies(store.BaseType, store.Type);
        }

        protected void AddRelationship<TEntity, TProperty, TOther>(Expression<Func<TEntity, TProperty>> memberExpr,
            Expression<Func<TEntity, TOther, bool>> relationshipExpr)
            where TProperty : IEnumerable<TOther>
        {
            var store = RelationshipsDict.GetValueOrDefault(typeof(TEntity));
            store ??= new TypeRelationship
            {
                Type = typeof(TEntity),
                BaseType = typeof(TEntity).GetRealBaseType()
            };

            store.Relationships.Add(new Relationship
            {
                Member = MemberExtractor.ExtractSingleMember(memberExpr).Member,
                Property = memberExpr,
                Expression = relationshipExpr
            });

            RelationshipsDict[typeof(TEntity)] = store;
            UpdateTypeHierachies(store.BaseType, store.Type);
        }

        //TODO could we use a fluent pattern to help build composite keys
        protected void AddKey<TEntity, TOther>(Expression<Func<TEntity, TOther>> memberExpr)
        {
            var store = KeysDict.GetValueOrDefault(typeof(TEntity));
            store ??= new Key
            {
                Type = typeof(TEntity),
                BaseType = typeof(TEntity).GetRealBaseType()
            };

            store.Fields.Add(memberExpr);

            KeysDict[typeof(TEntity)] = store;
            UpdateTypeHierachies(store.BaseType, store.Type);
        }

        private void UpdateTypeHierachies(Type baseType, Type type)
        {
            _typeHierarchy.AddOrUpdate(baseType, _ => new HashSet<Type> { type, baseType },
               (_, o) =>
               {
                   o.Add(type);
                   return o;
               });
        }

        internal void GetAllTypesWithSharedBaseType<TBase>()
        {
            var baseType = typeof(TBase);
            if (_typeHierarchy.TryGetValue(baseType, out HashSet<Type> types))
            {
                foreach (var modelType in types)
                {
                    if (!TypeHelpers.ContainsKey(modelType))
                    {
                        TypeHelpers.AddOrUpdate(modelType, CreateTypeHelper(modelType, baseType, this), (_, h) => h);
                    }
                }
            }
        }

        private static ITypeHelper CreateTypeHelper(Type type, Type baseType, KeyBuilder keyBuilder)
        {
            Type generic = typeof(TypeHelper<,>);
            Type[] typeArgs = { type, baseType };
            Type constructed = generic.MakeGenericType(typeArgs);

            return Activator.CreateInstance(constructed, keyBuilder) as ITypeHelper;
        }

        public IEnumerable<Relationship> GetRelationships<T>()
            => RelationshipsDict.GetValueOrDefault(typeof(T))?.Relationships ?? Enumerable.Empty<Relationship>();

        public IEnumerable<LambdaExpression> GetKeys<T>() 
            => KeysDict.GetValueOrDefault(typeof(T))?.Fields ?? Enumerable.Empty<LambdaExpression>();
    }


    public class Key
    {
        //TODO make immutable
        public Type Type { get; set; }

        //TODO make immutable
        public Type BaseType { get; set; }
        public List<LambdaExpression> Fields { get; set; } = new List<LambdaExpression>();
    }

    public class TypeRelationship
    {
        //TODO make immutable
        public Type Type { get; set; }

        //TODO make immutable
        public Type BaseType { get; set; }
        public List<Relationship> Relationships { get; set; } = new List<Relationship>();
    }

    //TODO make immutable
    public class Relationship
    {
        public MemberInfo Member { get; set; }
        public LambdaExpression Expression { get; set; }
        public LambdaExpression Property { get; set; }
    }
}
