using System.Collections.Generic;

namespace NobleTitlesPlus.extensions
{
    public static class DictionaryExt
    {
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            TValue value;
            if (key == null || !dictionary.TryGetValue(key, out value))
            {
                value = defaultValue;
            }
            return value;
        }
        public static void UpdateKeyValuePair<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue val)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = val;
            }
            else
            {
                dictionary.Add(key, val);
            }
        }
    }
}
