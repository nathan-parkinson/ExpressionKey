using System;

namespace ExpressionKey.Cache
{
    class CollectionInitializer<TClass> : ICollectionInitializer
    {
        public CollectionInitializer(Action<TClass> intializer)
        {
            Initializer = intializer;
        }

        public Action<TClass> Initializer { get; }
    }

}
