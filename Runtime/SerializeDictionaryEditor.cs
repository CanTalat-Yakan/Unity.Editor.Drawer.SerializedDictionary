#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEssentials
{
    [Serializable]
    public partial class SerializeDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
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