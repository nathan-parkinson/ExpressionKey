using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpressionKey
{
    internal interface IEntityStore { }

    internal interface IEntityStore<T> : IEntityStore
    {
        IEnumerable<T> GetValues();
        void AddEntities(IEnumerable<T> entities);

        bool TryGetEntity(T key, out T value);
    }

    internal class EntityStore<T> : HashSet<T>, IEntityStore<T>
    {
        public EntityStore(IEnumerable<T> entities, IEqualityComparer<T> comparer) : base(entities, comparer)
        {

        }

        IEnumerable<T> IEntityStore<T>.GetValues() => this;
        void IEntityStore<T>.AddEntities(IEnumerable<T> entities) => UnionWith(entities);
        bool IEntityStore<T>.TryGetEntity(T key, out T value) => TryGetValue(key, out value);
    }

#if NETSTANDARD2_0
    internal class EntityStoreDict<T> : Dictionary<T, T>, IEntityStore<T>
    {
        public EntityStoreDict(IEnumerable<T> entities, IEqualityComparer<T> comparer)
            : base(comparer)
        {
            ((IEntityStore<T>)this).AddEntities(entities);
        }

        IEnumerable<T> IEntityStore<T>.GetValues() => Keys;
        void IEntityStore<T>.AddEntities(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                try
                {
                    Add(entity, entity);
                }
                catch (ArgumentException)
                {
                    //entity already exists in store so we'll keep the orginal and move on to
                    //process the next entity
                }
            }
        }

        bool IEntityStore<T>.TryGetEntity(T key, out T value) => TryGetValue(key, out value);
    }
#endif
}