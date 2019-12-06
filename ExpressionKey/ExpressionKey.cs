namespace ExpressionKey
{
    public static class ExpressionKey
    {
        public static ExpressionKey<TKey, TValue> CreateKey<TKey, TValue>(TKey key)
            => new ExpressionKey<TKey, TValue>(key, default, true);

        public static ExpressionKey<TKey, TValue> CreateValue<TKey, TValue>(TValue value)
            => new ExpressionKey<TKey, TValue>(default, value, false);
    }

    public struct ExpressionKey<TKey, TValue>
    {
        public ExpressionKey(TKey keyItem, TValue value, bool isKey)
        {
            KeyItem = keyItem;
            ValueItem = value;
            IsKey = isKey;
        }

        public ExpressionKey(TKey keyItem)
        {
            KeyItem = keyItem;
            ValueItem = default;
            IsKey = true;
        }

        public ExpressionKey(TValue valueItem)
        {
            KeyItem = default;
            ValueItem = valueItem;
            IsKey = false;
        }

        public bool IsKey { get; }
        public TKey KeyItem { get; }
        public TValue ValueItem { get; }
    }
}
