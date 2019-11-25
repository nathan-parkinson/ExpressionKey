using ExpressionKey.Cache;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ExpressionKey
{

    internal class TypeHelper<T> : ITypeHelper
    {
        private static Dictionary<MemberInfo, Action<EntityPool>> memberSetters;

        public TypeHelper(KeyBuilder builder)
        {
            if (memberSetters == null)
            {
                memberSetters = new Dictionary<MemberInfo, Action<EntityPool>>();
                //set member setters
                var fkList = builder.GetForeignKeys<T>();
                foreach (var fk in fkList.Where(x => x.Expression.Body.Type == typeof(bool) && 
                                                        x.Expression.Parameters.Count == 2))
                {
                    var paramPool = Expression.Parameter(typeof(EntityPool));
                    var param = Expression.Parameter(typeof(T), "source");

                    var paramFuncType = typeof(Func<,>).MakeGenericType(typeof(T), fk.Member.GetMemberUnderlyingType());
                    var paramExprType = typeof(Expression<>).MakeGenericType(paramFuncType);


                    var propertyAccess = Expression.PropertyOrField(param, fk.Member.Name);

                    var entities = Expression.Call(paramPool, nameof(EntityPool.GetEntities), new Type[] { typeof(T) });
                    var otherEntities = Expression.Call(paramPool, nameof(EntityPool.GetEntities), new Type[]
                    {
                        fk.Expression.Parameters[1].Type
                    });


                    var typeParameters = new List<Type>
                    {
                        typeof(T),
                        fk.Property.Body.Type
                    };

                    //if IEnurmeable add anoter type parameter so we call the right method
                    if(fk.Property.Body.Type.IsIEnumerable())
                    {
                        typeParameters.Add(fk.Property.Body.Type.GetTypeToUse());
                    }

                    var setReferences = Expression.Call(typeof(Extensions), nameof(Extensions.SetReferences),
                                                typeParameters.ToArray() , entities, fk.Property, otherEntities,
                                                fk.Expression);

                    var lamdba = Expression.Lambda<Action<EntityPool>>(setReferences, paramPool);
                    var action = lamdba.Compile();

                    memberSetters.Add(fk.Member, action);
                }
            }
        }

        Type GetBaseType()
        {
            throw new NotImplementedException();
        }

        void ITypeHelper.SetReferences(EntityPool pool)
        {
            foreach (var setter in memberSetters)
            {
                setter.Value(pool);
            }
        }
    }
}
