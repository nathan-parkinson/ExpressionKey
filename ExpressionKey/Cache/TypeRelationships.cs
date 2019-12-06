using System;
using System.Collections.Generic;

namespace ExpressionKey.Cache
{
    public class TypeRelationships
    {
        public TypeRelationships(TypeRelationships key, Relationship fields)
        {
            Type = key.Type;
            BaseType = key.BaseType;
            Relationships.AddRange(key.Relationships);
            Relationships.Add(fields);
        }

        public TypeRelationships(Type type, Type baseType, Relationship fields)
        {
            Type = type;
            BaseType = baseType;
            Relationships.Add(fields);
        }

        public Type Type { get; }
        public Type BaseType { get; }
        public List<Relationship> Relationships { get; } = new List<Relationship>();
    }
}
