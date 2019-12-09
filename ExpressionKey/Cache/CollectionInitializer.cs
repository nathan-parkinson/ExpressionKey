using System;
using System.Collections.Generic;

namespace ExpressionKey.Cache
{
    class CollectionInitializer<TClass, TChild> : ICollectionInitializer
    {
        public CollectionInitializer(Func<TClass, IEnumerable<TChild>> intializer)
        {
            Initializer = intializer;
        }

        public Func<TClass, IEnumerable<TChild>> Initializer { get; }
    }

}
