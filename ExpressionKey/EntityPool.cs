using ExpressionKey.Cache;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ExpressionKey
{
    //Non Tread Safe
    public class EntityPool
    {
        internal readonly static ConcurrentDictionary<Type, ITypeHelper> _typeHelpers =
         new ConcurrentDictionary<Type, ITypeHelper>();

        //TODO add comparer store


        //TODO add code from Linq2db.include to use only baseTypes
        readonly ConcurrentDictionary<Type, IEnumerable> _entityStore =
         new ConcurrentDictionary<Type, IEnumerable>();

        private readonly KeyBuilder _keyBuilder;

        //internal readonly bool ConsolidateEntities = true;// Settings.ConsolidateEntities;

        public EntityPool(KeyBuilder keyBuilder)
        {
            _keyBuilder = keyBuilder;
        }


        public ISet<T> GetEntities<T>()
        {
            var baseType = typeof(T);
            if (!_entityStore.ContainsKey(baseType))
            {
                return null;
            }

            var entities = _entityStore[baseType] as ISet<T>;
            return entities;
        }

        public void AddEntities<T>(IEnumerable<T> entities)
        {
            var type = typeof(T);
            var pkFields = _keyBuilder.GetPrimaryKeys<T>();

            _entityStore.AddOrUpdate(type, _ => new HashSet<T>(entities, new PrimaryKeyComparer<T>(pkFields)),
                (_, o) => 
                {
                    
                    var oldHash = o as HashSet<T>;
                    oldHash.UnionWith(entities);
                    return oldHash;
                });

            if(!_typeHelpers.ContainsKey(type))
            {
                _typeHelpers.AddOrUpdate(type, CreateTypeHelper(type, _keyBuilder), (_, h) => h);
            }

            MatchEntities();
        }

        private static ITypeHelper CreateTypeHelper(Type type, KeyBuilder keyBuilder)
        {
            Type generic = typeof(TypeHelper<>);
            Type[] typeArgs = { type };
            Type constructed = generic.MakeGenericType(typeArgs);

            return Activator.CreateInstance(constructed, keyBuilder) as ITypeHelper;
        }

        private void MatchEntities()
        {
            foreach (var key in _entityStore.Keys)
            {
                if (_typeHelpers.TryGetValue(key, out ITypeHelper typeHelper))
                {
                    typeHelper.SetReferences(this);
                }
            }
        }
    }
}