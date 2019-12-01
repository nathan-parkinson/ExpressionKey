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

namespace ExpressionKey
{
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

            var relationship = new Relationship(
                MemberExtractor.ExtractSingleMember(memberExpr).Member,
                memberExpr,
                relationshipExpr);

            store = store == null ?
                new TypeRelationship(typeof(TEntity), typeof(TEntity).GetRealBaseType(), relationship) :
                new TypeRelationship(store, relationship);

            RelationshipsDict[typeof(TEntity)] = store;
            UpdateTypeHierachies(store.BaseType, store.Type);
        }

        protected void AddRelationship<TEntity, TProperty, TOther>(Expression<Func<TEntity, TProperty>> memberExpr,
            Expression<Func<TEntity, TOther, bool>> relationshipExpr)
            where TProperty : IEnumerable<TOther>
        {
            var store = RelationshipsDict.GetValueOrDefault(typeof(TEntity));

            var relationship = new Relationship(
                 MemberExtractor.ExtractSingleMember(memberExpr).Member,
                 memberExpr,
                 relationshipExpr);

            store = store == null ?
                new TypeRelationship(typeof(TEntity), typeof(TEntity).GetRealBaseType(), relationship) :
                new TypeRelationship(store, relationship);

            RelationshipsDict[typeof(TEntity)] = store;
            UpdateTypeHierachies(store.BaseType, store.Type);
        }

        //TODO could we use a fluent pattern to help build composite keys
        protected void AddKey<TEntity, TOther>(Expression<Func<TEntity, TOther>> memberExpr)
        {
            var store = KeysDict.GetValueOrDefault(typeof(TEntity));

            store = store == null ?
                new Key(typeof(TEntity), typeof(TEntity).GetRealBaseType(), memberExpr) :
                new Key(store, memberExpr);

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

        internal void CreateTypeHelperForAllTypesWithSharedBaseType<TBase>()
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
            var generic = typeof(TypeHelper<,>);
            Type[] typeArgs = { type, baseType };
            var constructed = generic.MakeGenericType(typeArgs);

            return Activator.CreateInstance(constructed, keyBuilder) as ITypeHelper;
        }

        public IEnumerable<Relationship> GetRelationships<T>()
            => RelationshipsDict.GetValueOrDefault(typeof(T))?.Relationships ?? Enumerable.Empty<Relationship>();

        public IEnumerable<LambdaExpression> GetKeys<T>()
            => KeysDict.GetValueOrDefault(typeof(T))?.Fields ?? Enumerable.Empty<LambdaExpression>();

        public KeyComparer<T> GetKeyComparer<T>()
        {
            var key = KeysDict[typeof(T)];
            if(key.KeyComparer == null)
            {
                key = new Key(key, new KeyComparer<T>(GetKeys<T>()));
                KeysDict[typeof(T)] = key;
            }

            return key.KeyComparer as KeyComparer<T>;
        }
    }


    public class Key
    {
        public Key(Key key, LambdaExpression fields)
        {
            Type = key.Type;
            BaseType = key.BaseType;
            Fields.AddRange(key.Fields);
            Fields.Add(fields);
        }

        public Key(Type type, Type baseType, LambdaExpression fields)
        {
            Type = type;
            BaseType = baseType;
            Fields.Add(fields);
        }
        public Key(Key key, IKeyComparer comparer)
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

    public class TypeRelationship
    {
        public TypeRelationship(TypeRelationship key, Relationship fields)
        {
            Type = key.Type;
            BaseType = key.BaseType;
            Relationships.AddRange(key.Relationships);
            Relationships.Add(fields);
        }

        public TypeRelationship(Type type, Type baseType, Relationship fields)
        {
            Type = type;
            BaseType = baseType;
            Relationships.Add(fields);
        }

        public Type Type { get; }
        public Type BaseType { get; }
        public List<Relationship> Relationships { get; } = new List<Relationship>();
    }

    public class Relationship
    {
        public Relationship(MemberInfo member, LambdaExpression property, LambdaExpression expression)
        {
            Member = member;
            Property = property;
            Expression = expression;
        }
        public MemberInfo Member { get; }
        public LambdaExpression Expression { get; }
        public LambdaExpression Property { get; }
    }
}
