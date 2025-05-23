#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Represents a serializable dictionary that supports serialization and deserialization of its key-value pairs.
    /// </summary>
    /// <remarks>This class implements <see cref="IDictionary{TKey, TValue}"/> for standard dictionary
    /// operations and  <see cref="ISerializationCallbackReceiver"/> to handle custom serialization logic. During
    /// serialization,  the dictionary's entries are converted into a serializable format. During deserialization, the
    /// entries  are reconstructed into the dictionary.  Special handling is applied for default keys during
    /// deserialization to ensure compatibility with common  key types such as <see cref="string"/>, <see cref="int"/>,
    /// and <see cref="float"/>. If a duplicate key  is encountered, a default key is generated and used
    /// instead.</remarks>
    /// <typeparam name="TKey">The type of the keys in the dictionary. Keys must be unique and cannot be null.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    [Serializable]
    public partial class SerializedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        public void OnBeforeSerialize()
        {
            _entries.Clear();
            foreach (var pair in _dictionary)
                _entries.Add(new SerializedKeyValuePair<TKey, TValue> { Key = pair.Key, Value = pair.Value });
        }

        public void OnAfterDeserialize()
        {
            _dictionary.Clear();

            foreach (var entry in _entries)
                if (entry.Key != null)
                    if (_dictionary.ContainsKey(entry.Key))
                    {
                        if (!_dictionary.ContainsKey(GetDefaultKey()))
                            _dictionary.Add(GetDefaultKey(), entry.Value);
                        else Debug.LogWarning("Unable to add new element to dictionary. Modify the default key implementation!");
                    }
                    else
                    {
                        _dictionary.Add(entry.Key, entry.Value);
                    }
        }

        private static TKey GetDefaultKey()
        {
            // Special cases for common types, else default(TKey)
            if (typeof(TKey) == typeof(string))
                return (TKey)(object)string.Empty;
            if (typeof(TKey) == typeof(Color))
                return (TKey)(object)Color.black;
            if (typeof(TKey) == typeof(int))
                return (TKey)(object)0;
            if (typeof(TKey) == typeof(float))
                return (TKey)(object)0f;
            if (typeof(TKey).IsValueType)
                return default;
            return default;
        }
    }

}
#endif