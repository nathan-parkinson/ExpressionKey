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

    internal interface ITypeHelper
    {
        Type Type { get; }
        Type BaseType { get; }
        void SetReferences(EntityPool pool);
    }
}
