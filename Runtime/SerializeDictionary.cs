using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace UnityEssentials
{
    [Serializable]
    public struct SerializedKeyValuePair<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;
    }

    public class SplitWeightAttribute : PropertyAttribute
    {
        public float KeyWeight = 1f;
        public SplitWeightAttribute(float keyWeight = 1f) => KeyWeight = keyWeight;
    }

    [Serializable]
    public class SerializeDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<SerializedKeyValuePair<TKey, TValue>> _entries = new();

        private Dictionary<TKey, TValue> _dictionary = new();

        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;
        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;

        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set => _dictionary[key] = value;
        }

        public void Add(TKey key, TValue value) => _dictionary.Add(key, value);
        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);
        public bool Remove(TKey key) => _dictionary.Remove(key);
        public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);
        public void Clear() => _dictionary.Clear();
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();

#if UNITY_EDITOR
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
                {
                    if (_dictionary.ContainsKey(entry.Key))
                        _dictionary.Add(GetDefaultKey(), entry.Value);
                    else
                        _dictionary.Add(entry.Key, entry.Value);
                }
        }
#else
        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { }
#endif

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

        // IDictionary explicit implementations
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => _dictionary.TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Remove(item);
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SerializeDictionary<,>))]
    public class SerializeDictionaryDrawer : PropertyDrawer
    {
        private ReorderableList _list;
        private SerializedProperty _entriesProperty;
        private float _keyWeight = 1;

        private void Initialize(SerializedProperty property)
        {
            if (_list != null)
                return;

            var targetObject = property.serializedObject.targetObject;
            var field = GetFieldByPath(targetObject.GetType(), property.propertyPath);
            var attribute = field?.GetCustomAttribute<SplitWeightAttribute>();
            _keyWeight = attribute?.KeyWeight ?? 1;

            _entriesProperty = property.FindPropertyRelative("_entries");

            _list = new ReorderableList(property.serializedObject, _entriesProperty)
            {
                drawElementCallback = DrawElement,
                elementHeightCallback = GetElementHeight,
                headerHeight = 0
            };
        }

        private float GetElementHeight(int index)
        {
            var element = _entriesProperty.GetArrayElementAtIndex(index);
            var keyProperty = element.FindPropertyRelative("Key");
            var valueProperty = element.FindPropertyRelative("Value");

            float spacing = 4f;

            float keyHeight = EditorGUI.GetPropertyHeight(keyProperty, GUIContent.none, true);
            float valueHeight = GetPropertyHeightRecursive(valueProperty);

            return Mathf.Max(keyHeight, valueHeight) + spacing;
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _entriesProperty.GetArrayElementAtIndex(index);
            var keyProperty = element.FindPropertyRelative("Key");
            var valueProperty = element.FindPropertyRelative("Value");

            rect.y += 2;
            rect.height -= 4;

            float totalWeight = _keyWeight + 1f;
            float keyPortion = _keyWeight / totalWeight;
            float spacing = 5f;
            float leftPadding = 12f;
            float rightPadding = 12f;

            float keyWidth = rect.width * keyPortion;
            float valueWidth = rect.width - keyWidth;

            var keyRect = new Rect(rect.x + leftPadding, rect.y, keyWidth - spacing - leftPadding, rect.height);
            var valueRect = new Rect(rect.x + keyWidth + spacing, rect.y, valueWidth - rightPadding - spacing, rect.height);

            EditorGUI.PropertyField(keyRect, keyProperty, GUIContent.none, true);

            if (IsCustomSerializableClass(valueProperty))
            {
                // Draw all visible children inside valueRect recursively
                DrawPropertyRecursive(valueRect, valueProperty);
            }
            else
            {
                EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Initialize(property);
            return property.isExpanded ? _list.GetHeight() + 21 : EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            Initialize(property);

            // Draw foldout with label
            var foldoutRect = new Rect(rect.x - 3, rect.y, rect.width - 48, EditorGUIUtility.singleLineHeight + 2);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true, EditorStyles.foldoutHeader);

            // Draw array size on the right
            var sizeRect = new Rect(rect.x + rect.width - 48, rect.y, 48, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            var size = EditorGUI.DelayedIntField(sizeRect, _entriesProperty.arraySize);
            if (EditorGUI.EndChangeCheck())
                _entriesProperty.arraySize = size;

            if (property.isExpanded)
            {
                rect.y += EditorGUIUtility.singleLineHeight + 3;
                rect.height -= EditorGUIUtility.singleLineHeight;
                _list.DoList(rect);
            }
        }

        private static FieldInfo GetFieldByPath(Type type, string path)
        {
            var parts = path.Split('.');
            FieldInfo field = null;

            foreach (var part in parts)
            {
                field = type.GetField(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null)
                    return null;

                type = field.FieldType;
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                    type = type.GetGenericArguments()[0];
            }

            return field;
        }

        private static bool IsCustomSerializableClass(SerializedProperty property)
        {
            if (property == null)
                return false;

            return property.propertyType == SerializedPropertyType.Generic
                && property.hasVisibleChildren
                && !property.type.StartsWith("PPtr<"); // Avoid UnityEngine.Object refs
        }

        private static void DrawPropertyRecursive(Rect rect, SerializedProperty property)
        {
            var prop = property.Copy();
            var endProperty = prop.GetEndProperty();

            float yOffset = rect.y;
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            // Save current label width
            float originalLabelWidth = EditorGUIUtility.labelWidth;

            // Reduce label width to give more space to input fields
            EditorGUIUtility.labelWidth = 175f; // Adjust as you like, smaller means wider input fields

            bool enterChildren = true;
            while (prop.NextVisible(enterChildren) && !SerializedProperty.EqualContents(prop, endProperty))
            {
                float height = EditorGUI.GetPropertyHeight(prop, true);
                var childRect = new Rect(rect.x, yOffset, rect.width, height);
                EditorGUI.PropertyField(childRect, prop, true);
                yOffset += height + EditorGUIUtility.standardVerticalSpacing;
                enterChildren = false;
            }

            // Restore original label width
            EditorGUIUtility.labelWidth = originalLabelWidth;

            EditorGUI.indentLevel = indent;
        }

        private static float GetPropertyHeightRecursive(SerializedProperty property)
        {
            var prop = property.Copy();
            var endProperty = prop.GetEndProperty();

            float totalHeight = 0f;

            bool enterChildren = true;
            while (prop.NextVisible(enterChildren) && !SerializedProperty.EqualContents(prop, endProperty))
            {
                totalHeight += EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
                enterChildren = false;
            }

            return totalHeight > 0 ? totalHeight - EditorGUIUtility.standardVerticalSpacing : totalHeight; // Remove last spacing
        }
    }
#endif
}