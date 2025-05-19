#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEssentials
{
    [CustomPropertyDrawer(typeof(SerializedDictionary<,>))]
    public class SerializeDictionaryDrawer : PropertyDrawer
    {
        private ReorderableList _list;
        private SerializedProperty _entriesProperty;
        private float _keyWeight = 1;

        private void Initialize(SerializedProperty property)
        {
            if (_list != null)
                return;

            var field = GetSerializedFieldInfo(property);
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
                // Draw all visible children inside valueRect recursively
                DrawPropertyRecursive(valueRect, valueProperty);
            else EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none, true);
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
            var foldoutRect = new Rect(rect.x - 3, rect.y, rect.width *10, EditorGUIUtility.singleLineHeight + 2);
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

        private static FieldInfo GetSerializedFieldInfo(SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject;
            var pathSegment = property.propertyPath.Split('.');
            var fieldInfo = (FieldInfo)null;
            var currentType = targetObject.GetType();

            foreach (var segment in pathSegment)
            {
                // Skip array data paths
                if (segment.StartsWith("Array.data["))
                    continue;

                fieldInfo = currentType.GetField(segment, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fieldInfo == null)
                    return null;

                currentType = fieldInfo.FieldType;
            }

            return fieldInfo;
        }
    }
}
#endif