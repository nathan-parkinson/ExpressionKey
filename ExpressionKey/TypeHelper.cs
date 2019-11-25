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
                var fk = builder.GetForeignKeys<T>();
                foreach (var (member, expression, property) in fk.Where(x => x.expression.Body.Type == typeof(bool) && x.expression.Parameters.Count == 2))
                {
                    //build Action<EntityPool> here for 
                    //need 
                    // member := Expression<Func<T, U>>, 
                    // fk expression := Expression<Func<T, U, bool>>, key.expression
                    // other entities:= IEnumerable<U>


                    ///////////////////////////////////////////////////

                    var paramPool = Expression.Parameter(typeof(EntityPool));
                    var param = Expression.Parameter(typeof(T), "source");

                    var paramFuncType = typeof(Func<,>).MakeGenericType(typeof(T), member.GetMemberUnderlyingType());
                    var paramExprType = typeof(Expression<>).MakeGenericType(paramFuncType);


                    var propertyAccess = Expression.PropertyOrField(param, member.Name);

                    var entities = Expression.Call(paramPool, nameof(EntityPool.GetEntities), new Type[] { typeof(T) });
                    var otherEntities = Expression.Call(paramPool, nameof(EntityPool.GetEntities), new Type[] { expression.Parameters[1].Type });


                    var typeParameters = new List<Type>
                    {
                        typeof(T),
                        property.Body.Type
                    };

                    //if IEnurmeable add anoter type parameter so we call the right method
                    if(property.Body.Type.IsIEnumerable())
                    {
                        typeParameters.Add(property.Body.Type.GetTypeToUse());
                    }

                    var setReferences = Expression.Call(typeof(Extensions), nameof(Extensions.SetReferences),
                                                typeParameters.ToArray() , entities, property, otherEntities,
                                                expression);

                    var lamdba = Expression.Lambda<Action<EntityPool>>(setReferences, paramPool);
                    var action = lamdba.Compile();

                    memberSetters.Add(member, action);
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
