using ExpressionKey.Cache;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionKey
{
    //Non Tread Safe
    public class EntityPool
    {
        //TODO add comparer store

        private readonly KeyBuilder _keyBuilder;
        private readonly ConcurrentDictionary<Type, IEnumerable> _entityStore
            = new ConcurrentDictionary<Type, IEnumerable>();

        //internal readonly bool ConsolidateEntities = true;// Settings.ConsolidateEntities;

        public EntityPool(KeyBuilder keyBuilder)
        {
            _keyBuilder = keyBuilder;
        }

        internal IEnumerable<T> GetEntities<T, TBase>()
        {
            var baseType = typeof(TBase);
            if (!_entityStore.ContainsKey(baseType))
            {
                return null;
            }

            var entities = _entityStore[baseType] as ISet<TBase>;
            return entities.OfType<T>();
        }

        public IEnumerable<T> GetEntities<T>() => BaseTypeRouter<T>.GetEntities(this);
        public void AddEntities<T>(IEnumerable<T> entities) => BaseTypeRouter<T>.AddEntities(this, entities);
        
        internal void AddEntities<T, TBase>(IEnumerable<T> entities)
        {
            var baseType = typeof(TBase);
            var type = typeof(T);

            var pkFields = _keyBuilder.GetKeys<TBase>();
            var baseEntities = entities.Cast<TBase>();

            _entityStore.AddOrUpdate(baseType, _ => new HashSet<TBase>(baseEntities, new KeyComparer<TBase>(pkFields)),
                (_, o) =>
                {
                    var oldHash = o as HashSet<TBase>;
                    oldHash.UnionWith(baseEntities);
                    return oldHash;
                });

            _keyBuilder.GetAllTypesWithSharedBaseType<TBase>();
            MatchEntities();
        }

        private void MatchEntities()
        {
            foreach (var typeHelper in _keyBuilder.TypeHelpers)
            {
                //is the below if really needed
                if (_entityStore.ContainsKey(typeHelper.Value.BaseType))
                {
                    typeHelper.Value.SetReferences(this);
                }
            }
        }
    }
}