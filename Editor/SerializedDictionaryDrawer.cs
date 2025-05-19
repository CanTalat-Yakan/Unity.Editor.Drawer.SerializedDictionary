#if UNITY_EDITOR
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

        private float _indentOffset => 16 * EditorGUI.indentLevel;

        private void Initialize(SerializedProperty property)
        {
            if (_list != null)
                return;

            InspectorHookUtilities.TryGetAttribute<SplitWeightAttribute>(property, out var attribute);
            _keyWeight = attribute?.KeyWeight ?? 1;

            _entriesProperty = property.FindPropertyRelative("_entries");

            _list = new ReorderableList(property.serializedObject, _entriesProperty)
            {
                drawElementCallback = DrawElement,
                elementHeightCallback = GetElementHeight,
                headerHeight = 0
            };

            InspectorHook.MarkPropertyAsHandled(property.propertyPath);
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

            float keyHeight = EditorGUI.GetPropertyHeight(keyProperty, GUIContent.none, true);
            float valueHeight = EditorGUI.GetPropertyHeight(valueProperty, GUIContent.none, true);

            float keyWidth = rect.width * keyPortion;
            float valueWidth = rect.width - keyWidth;

            var keyRect = new Rect(rect.x + leftPadding, rect.y, keyWidth - spacing - leftPadding, keyHeight);
            var valueRect = new Rect(rect.x + keyWidth + spacing, rect.y, valueWidth - rightPadding - spacing, valueHeight);

            InspectorHook.DrawProperty(keyRect, keyProperty, GUIContent.none, true);

            if (!InspectorHookUtilities.IsGenericPropertyWithChildren(valueProperty))
                InspectorHook.DrawProperty(valueRect, valueProperty, GUIContent.none, true);
            else if (valueProperty.NextVisible(true)) // Skip script foldout
                InspectorHookUtilities.Iterate(valueProperty, (childProperty) =>
                {
                    InspectorHook.DrawProperty(valueRect, childProperty, GUIContent.none, true);

                    float height = EditorGUI.GetPropertyHeight(childProperty, true);
                    valueRect.y += height + EditorGUIUtility.standardVerticalSpacing;
                });

            InspectorHook.MarkPropertyAsHandled(element.propertyPath);
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

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Initialize(property);
            return property.isExpanded ? _list.GetHeight() + 21 : EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            Initialize(property);

            // Draw foldout with label
            var foldoutRect = new Rect(rect.x - 3, rect.y, rect.width, EditorGUIUtility.singleLineHeight + 2);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true, EditorStyles.foldoutHeader);

            // Draw array size on the right
            var intFieldWidth = 76;
            var sizeRect = new Rect(rect.x + rect.width - _indentOffset - intFieldWidth, rect.y, _indentOffset + intFieldWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            var size = EditorGUI.DelayedIntField(sizeRect, _entriesProperty.arraySize);
            if (EditorGUI.EndChangeCheck())
                _entriesProperty.arraySize = size;

            if (property.isExpanded)
            {
                rect.x += _indentOffset;
                rect.y += EditorGUIUtility.singleLineHeight + 3;
                rect.width -= _indentOffset;
                rect.height -= EditorGUIUtility.singleLineHeight;
                _list.DoList(rect);
            }
        }

        private static float GetPropertyHeightRecursive(SerializedProperty property)
        {
            var startProperty = property.Copy();
            var endProperty = startProperty.GetEndProperty();

            float totalHeight = 0f;

            bool enterChildren = true;
            while (startProperty.NextVisible(enterChildren) && !SerializedProperty.EqualContents(startProperty, endProperty))
            {
                totalHeight += EditorGUI.GetPropertyHeight(startProperty, true) + EditorGUIUtility.standardVerticalSpacing;
                enterChildren = false;
            }

            return totalHeight > 0 ? totalHeight - EditorGUIUtility.standardVerticalSpacing : totalHeight; // Remove last spacing
        }
    }
}
#endif