using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpressionKey.Stores
{
    internal interface IEntityStore<T> : IEntityStore
    {
        IEnumerable<T> GetValues();
        void AddEntities(IEnumerable<T> entities);

        bool TryGetEntity(T key, out T value);
    }
}