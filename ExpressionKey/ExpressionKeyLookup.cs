﻿using ExpressionKey.Comparers;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionKey
{
    public class ExpressionKeyLookup<T, U> : IExpressionKeyLookup<T, U>
    {
        private static readonly ConcurrentDictionary<Expression, IRelationshipComparer> _comparerStore =
            new ConcurrentDictionary<Expression, IRelationshipComparer>(ExpressionEqualityComparer.Instance);

        private readonly ILookup<ExpressionKey<T, U>, T> _lookup;

        internal ExpressionKeyLookup(IEnumerable<T> items, Expression<Func<T, U, bool>> expr)
        {
            var comparer = _comparerStore.GetOrAdd(expr, e =>
            {
                var ex = e as Expression<Func<T, U, bool>>;
                return new RelationshipComparer<T, U>(ex);
            }) as RelationshipComparer<T, U>;

            IsExpressionInvalid = comparer.IsExpressionInvalid;

            if (!comparer.IsExpressionInvalid)
            {
                _lookup = items.ToLookup(x => new ExpressionKey<T, U>(x), comparer);
            }
        }

        internal bool IsExpressionInvalid { get; }

        public IEnumerable<T> this[T key] => this[new ExpressionKey<T, U>(key)];
        public IEnumerable<T> GetMatches(U key) => _lookup[ExpressionKey.CreateValue<T, U>(key)];
        public IEnumerable<T> this[ExpressionKey<T, U> key] => _lookup[key];

        public int Count => _lookup.Count;

        public bool Contains(T key) => _lookup.Contains(ExpressionKey.CreateKey<T, U>(key));
        public bool Contains(U key) => _lookup.Contains(ExpressionKey.CreateValue<T, U>(key));
        public bool Contains(ExpressionKey<T, U> key) => _lookup.Contains(key);

        public IEnumerator<IGrouping<ExpressionKey<T, U>, T>> GetEnumerator() => _lookup.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_lookup).GetEnumerator();
    }
}
