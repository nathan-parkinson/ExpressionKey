using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ExpressionKey
{
    public class EntityPool : IEntityPool
    {
        private readonly KeyBuilder _keyBuilder;
        private readonly ConcurrentDictionary<Type, IEntityStore> _entityStore
            = new ConcurrentDictionary<Type, IEntityStore>();
        
        internal EntityPool(KeyBuilder keyBuilder)
        {
            _keyBuilder = keyBuilder;
        }

        internal IEnumerable<T> GetAllEntities<T, TBase>()
        {
            var baseType = typeof(TBase);
            if (!_entityStore.ContainsKey(baseType))
            {
                return null;
            }

            var entities = _entityStore[baseType] as IEntityStore<TBase>;
            return entities.GetValues().OfType<T>();
        }

        public IEnumerable<T> GetAllEntities<T>() => BaseTypeRouter<T>.GetAllEntities(this);
        public void AddEntities<T>(IEnumerable<T> entities) => BaseTypeRouter<T>.AddEntities(this, entities);

        internal void AddEntities<T, TBase>(IEnumerable<T> entities)
        {
            var baseType = typeof(TBase);
            var type = typeof(T);

            var baseEntities = entities.Cast<TBase>();
            
            _entityStore.AddOrUpdate(baseType, _ => new EntityStore<TBase>(baseEntities, _keyBuilder.GetKeyComparer<TBase>()),
                (_, o) =>
                {
                    var oldHash = o as IEntityStore<TBase>;
                    oldHash.AddEntities(baseEntities);
                    return oldHash;
                });

            _keyBuilder.CreateTypeHelperForAllTypesWithSharedBaseType<TBase>();
            MatchEntities();
        }

        public IEnumerable<T> GetEntities<T>(IEnumerable<T> entities)
            => BaseTypeRouter<T>.GetEntities(this, entities);

        internal List<T> GetEntities<T, TBase>(IEnumerable<T> entities)
        {
            AddEntities<T, TBase>(entities);
            if (!_entityStore.TryGetValue(typeof(TBase), out IEntityStore uniqueEntities))
            {
                throw new ArgumentException($"Entities of type '{typeof(TBase).Name}' could not be found");
            }

            var deDupedHash = uniqueEntities as IEntityStore<TBase>;
            var result = entities.Cast<TBase>().Select(e =>
            {
                if (deDupedHash.TryGetEntity(e, out TBase x))
                {
                    return x;
                }
                return e;
            }).Cast<T>().ToList();

            return result;
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