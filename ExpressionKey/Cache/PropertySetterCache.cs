using System;

namespace ExpressionKey.Cache
{
    class PropertySetterCache<TElement, TValue> : IPropertySetterCache
    {
        public PropertySetterCache(Action<TElement, TValue> setter)
        {
            Setter = setter;
        }

        public Action<TElement, TValue> Setter { get; }
    }
}
