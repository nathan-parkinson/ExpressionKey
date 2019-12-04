using System;

namespace ExpressionKey
{

    internal interface ITypeHelper
    {
        Type Type { get; }
        Type BaseType { get; }
        void SetReferences(EntityPool pool);
    }
}
