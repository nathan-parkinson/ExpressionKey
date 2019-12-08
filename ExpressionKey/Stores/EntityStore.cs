using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpressionKey.Stores
{
#if (NETSTANDARD2_1 || NET472 || NET48 || NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP2_2 || NETCOREAPP3_0 || NETCOREAPP3_1)
    internal class EntityStore<T> : HashSet<T>, IEntityStore<T>
    {
        public EntityStore(IEnumerable<T> entities, IEqualityComparer<T> comparer) : base(entities, comparer)
        { }

        IEnumerable<T> IEntityStore<T>.GetValues() => this;
        void IEntityStore<T>.AddEntities(IEnumerable<T> entities) => UnionWith(entities);
        bool IEntityStore<T>.TryGetEntity(T key, out T value) => TryGetValue(key, out value);
    }
#else
    internal class EntityStore<T> : Dictionary<T, T>, IEntityStore<T>
    {
        public EntityStore(IEnumerable<T> entities, IEqualityComparer<T> comparer) : base(comparer)
            => ((IEntityStore<T>)this).AddEntities(entities);
    
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