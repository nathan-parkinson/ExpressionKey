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

        internal IEnumerable<T> GetEntities<T, TBase>()
        {
            var baseType = typeof(TBase);
            if (!_entityStore.ContainsKey(baseType))
            {
                return null;
            }

            var entities = _entityStore[baseType] as ISet<TBase>;
            return entities.OfType<T>();
        }

        public IEnumerable<T> GetEntities<T>() => BaseTypeRouter<T>.GetEntities(this);
        public void AddEntities<T>(IEnumerable<T> entities) => BaseTypeRouter<T>.AddEntities(this, entities);
        
        internal void AddEntities<T, TBase>(IEnumerable<T> entities)
        {
            var baseType = typeof(TBase);
            var type = typeof(T);

            var pkFields = _keyBuilder.GetPrimaryKeys<TBase>();
            var baseEntities = entities.Cast<TBase>();

            _entityStore.AddOrUpdate(baseType, _ => new HashSet<TBase>(baseEntities, new PrimaryKeyComparer<TBase>(pkFields)),
                (_, o) =>
                {

                    var oldHash = o as HashSet<TBase>;
                    oldHash.UnionWith(baseEntities);
                    return oldHash;
                });


            foreach(var modelType in type.GetAllBaseTypes())
            {
                if (!_typeHelpers.ContainsKey(modelType))
                {
                    _typeHelpers.AddOrUpdate(modelType, CreateTypeHelper(modelType, baseType, _keyBuilder), (_, h) => h);
                }
            }

            MatchEntities();
        }

        private static ITypeHelper CreateTypeHelper(Type type, Type baseType, KeyBuilder keyBuilder)
        {
            Type generic = typeof(TypeHelper<,>);
            Type[] typeArgs = { type, baseType };
            Type constructed = generic.MakeGenericType(typeArgs);

            return Activator.CreateInstance(constructed, keyBuilder) as ITypeHelper;
        }


        private void MatchEntities()
        {
            foreach (var typeHelper in _typeHelpers)
            {
                //is the below if really needed
                if (_entityStore.ContainsKey(typeHelper.Value.BaseType))
                {
                    typeHelper.Value.SetReferences(this);
                }
            }
        }
    }
}