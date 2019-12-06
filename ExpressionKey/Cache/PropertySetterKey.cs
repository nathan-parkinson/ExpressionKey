using System;
using System.Collections.Generic;

namespace ExpressionKey.Cache
{
    internal class PropertySetterKey : IEquatable<PropertySetterKey>
    {
        internal PropertySetterKey(Type elementType, Type valueType, string propertyName)
        {
            ElementType = elementType;
            ValueType = valueType;
            PropertyName = propertyName;
        }

        public Type ElementType { get; }
        public Type ValueType { get; }
        public string PropertyName { get; }

        public override bool Equals(object obj) => obj is PropertySetterKey key && Equals(key);

        public bool Equals(PropertySetterKey key) 
            => EqualityComparer<Type>.Default.Equals(ElementType, key.ElementType) &&
                   EqualityComparer<Type>.Default.Equals(ValueType, key.ValueType) &&
                   PropertyName == key.PropertyName;

        public override int GetHashCode() => HashCode.Combine(ElementType, ValueType, PropertyName);
    }
}
