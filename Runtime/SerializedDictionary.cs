using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Represents a serializable key-value pair.
    /// </summary>
    /// <remarks>This struct is designed to be used in scenarios where key-value pairs need to be serialized,
    /// such as when persisting data or transmitting it over a network.</remarks>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    [Serializable]
    public struct SerializedKeyValuePair<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;
    }

    /// <summary>
    /// Specifies the relative weight of the key portion in a key-value split layout.
    /// </summary>
    /// <remarks>This attribute is used to adjust the proportional width of the key in a key-value pair
    /// layout. The weight determines how much space the key occupies relative to the value.</remarks>
    public class KeyValueSplitWeightAttribute : PropertyAttribute
    {
        public float KeyWeight = 1f;
        public KeyValueSplitWeightAttribute(float keyWeight = 1f) => KeyWeight = keyWeight;
    }

    /// <summary>
    /// Represents a dictionary that can be serialized, supporting both key-value pair storage and serialization for use
    /// in contexts such as Unity or other frameworks requiring serialized data.
    /// </summary>
    /// <remarks>This class combines the functionality of a standard <see cref="Dictionary{TKey, TValue}"/>
    /// with serialization support by maintaining an internal list of key-value pairs. It is particularly useful in
    /// scenarios where dictionaries need to be serialized, such as in Unity's serialization system.  The dictionary
    /// provides all standard dictionary operations, including adding, removing, and retrieving key-value pairs, as well
    /// as enumerating the collection. The internal serialized list ensures that the dictionary's state can be persisted
    /// and restored during serialization.</remarks>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    public partial class SerializedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        [SerializeField]
        private List<SerializedKeyValuePair<TKey, TValue>> _entries = new();

        public Dictionary<TKey, TValue> Dictionary => _dictionary;
        private Dictionary<TKey, TValue> _dictionary = new();

        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;
        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets or sets the value associated with the specified key in the dictionary.
        /// </summary>
        /// <param name="key">The key whose associated value is to be retrieved or set. Cannot be <see langword="null"/>.</param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set => _dictionary[key] = value;
        }

        public void TryAdd(TKey key, TValue value)
        {
            if (!_dictionary.ContainsKey(key))
                _dictionary.Add(key, value);
        }
        public void TryGetValue(TKey key, TValue defaultValue, out TValue value)
        {
            if (!_dictionary.TryGetValue(key, out value))
                value = defaultValue;
        }
        public void Add(TKey key, TValue value) => _dictionary.Add(key, value);
        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);
        public bool Remove(TKey key) => _dictionary.Remove(key);
        public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);
        public void Clear() => _dictionary.Clear();
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();

        // IDictionary explicit implementations
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => _dictionary.TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Remove(item);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
            ((IDictionary<TKey, TValue>)Dictionary).CopyTo(array, arrayIndex);

        public void CopyFrom(IDictionary<TKey, TValue> source)
        {
            if (source == null)
                return;

            Clear();
            foreach (var kvp in source)
                _dictionary[kvp.Key] = kvp.Value;
        }

        public void AddFrom(IDictionary<TKey, TValue> source)
        {
            if (source == null)
                return;

            foreach (var kvp in source)
                if (!_dictionary.ContainsKey(kvp.Key))
                    _dictionary[kvp.Key] = kvp.Value;
        }
    }
}