using ExpressionKey.Cache;
using ExpressionKey.Comparers;
using ExpressionKey.Visitors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionKey
{
    public abstract class KeyBuilder
    {
        internal readonly ConcurrentDictionary<Type, ITypeHelper> TypeHelpers =
            new ConcurrentDictionary<Type, ITypeHelper>();

        private readonly ConcurrentDictionary<Type, HashSet<Type>> _typeHierarchy =
            new ConcurrentDictionary<Type, HashSet<Type>>();

        protected readonly ConcurrentDictionary<Type, TypeRelationships> RelationshipsStore =
            new ConcurrentDictionary<Type, TypeRelationships>();

        protected readonly ConcurrentDictionary<Type, KeyDetails> KeysStore
            = new ConcurrentDictionary<Type, KeyDetails>();

        public IEntityPool CreateEntityPool() => new EntityPool(this);

        protected void AddRelationship<TEntity, TOther>(Expression<Func<TEntity, TOther>> memberExpr,
            Expression<Func<TEntity, TOther, bool>> relationshipExpr)
        {
            var relationship = new Relationship(
                MemberExtractor.ExtractSingleMember(memberExpr).Member,
                memberExpr,
                relationshipExpr);

            var store = RelationshipsStore.AddOrUpdate(typeof(TEntity),
                new TypeRelationships(typeof(TEntity), typeof(TEntity).GetRealBaseType(), relationship),
                (_, tr) => new TypeRelationships(tr, relationship));

            UpdateTypeHierachies(store.BaseType, store.Type);
        }

        protected void AddRelationship<TEntity, TEnumerable, TOther>(Expression<Func<TEntity, TEnumerable>> memberExpr,
            Expression<Func<TEntity, TOther, bool>> relationshipExpr)
            where TEnumerable : IEnumerable<TOther>
        {
            var relationship = new Relationship(
                MemberExtractor.ExtractSingleMember(memberExpr).Member,
                memberExpr,
                relationshipExpr);

            var store = RelationshipsStore.AddOrUpdate(typeof(TEntity),
                new TypeRelationships(typeof(TEntity), typeof(TEntity).GetRealBaseType(), relationship),
                (_, tr) => new TypeRelationships(tr, relationship));

            UpdateTypeHierachies(store.BaseType, store.Type);
        }

        //TODO could we use a fluent pattern to help build composite keys
        protected void AddKey<TEntity, TOther>(Expression<Func<TEntity, TOther>> memberExpr)
        {
            var store = KeysStore.AddOrUpdate(typeof(TEntity),
                            new KeyDetails(typeof(TEntity), typeof(TEntity).GetRealBaseType(), memberExpr),
                            (_, k) => new KeyDetails(k, memberExpr));

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
        {
            if(RelationshipsStore.TryGetValue(typeof(T), out TypeRelationships tr))
            {
                return tr?.Relationships ?? Enumerable.Empty<Relationship>();
            }

            return Enumerable.Empty<Relationship>();
        }

        public IEnumerable<LambdaExpression> GetKeys<T>()
        {
            if(KeysStore.TryGetValue(typeof(T), out KeyDetails key))
            {
                return key?.Fields ?? Enumerable.Empty<LambdaExpression>();
            }

            return Enumerable.Empty<LambdaExpression>();
        }

        public KeyComparer<T> GetKeyComparer<T>()
        {
            if(KeysStore.TryGetValue(typeof(T), out KeyDetails key))
            {
                if (key.KeyComparer == null)
                {
                    var comparer = new KeyComparer<T>(GetKeys<T>());
                    var newKey = new KeyDetails(key, comparer);
                    KeysStore.TryUpdate(typeof(T), newKey, key);

                    return comparer;
                }

                return key.KeyComparer as KeyComparer<T>;
            }

            return null;
        }
    }
}
