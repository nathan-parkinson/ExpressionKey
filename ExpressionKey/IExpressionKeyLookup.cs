using System.Collections.Generic;
using System.Linq;

namespace ExpressionKey
{
    public interface IExpressionKeyLookup<T, U> : ILookup<ExpressionKey<T, U>, T>
    {
        IEnumerable<T> this[T key] { get; }
        bool Contains(T key);
        bool Contains(U key);
        IEnumerable<T> GetMatches(U key);
    }
}