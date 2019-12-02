using ExpressionKey.Cache;
using ExpressionKey.Comparers;
using ExpressionKey.Visitors;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

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
