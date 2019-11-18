using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
