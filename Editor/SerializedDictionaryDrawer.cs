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

        private void DrawElement(Rect position, int index, bool isActive, bool isFocused)
        {
            var element = _entriesProperty.GetArrayElementAtIndex(index);
            var keyProperty = element.FindPropertyRelative("Key");
            var valueProperty = element.FindPropertyRelative("Value");

            position.y += 2;
            position.height -= 4;

            float totalWeight = _keyWeight + 1f;
            float keyPortion = _keyWeight / totalWeight;
            float spacing = 5f;
            float leftPadding = 12f;
            float rightPadding = 12f;

            float keyHeight = EditorGUI.GetPropertyHeight(keyProperty, GUIContent.none, true);
            float valueHeight = EditorGUI.GetPropertyHeight(valueProperty, GUIContent.none, true);

            float keyWidth = position.width * keyPortion;
            float valueWidth = position.width - keyWidth;

            var keyPosition = new Rect(position.x + leftPadding, position.y, keyWidth - spacing - leftPadding, keyHeight);
            var valuePosition = new Rect(position.x + keyWidth + spacing, position.y, valueWidth - rightPadding - spacing, valueHeight);

            InspectorHook.DrawProperty(keyPosition, keyProperty, GUIContent.none, true);

            if (!InspectorHookUtilities.IsGenericWithChildren(valueProperty))
                InspectorHook.DrawProperty(valuePosition, valueProperty, GUIContent.none, true);
            else if (valueProperty.NextVisible(true)) // Skip script foldout
                InspectorHookUtilities.Iterate(valueProperty, (childProperty) =>
                {
                    InspectorHook.DrawProperty(valuePosition, childProperty, GUIContent.none, true);

                    float height = EditorGUI.GetPropertyHeight(childProperty, true);
                    valuePosition.y += height + EditorGUIUtility.standardVerticalSpacing;
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

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize(property);

            // Draw foldout with label
            var foldoutPosition = new Rect(position.x - 3, position.y, position.width, EditorGUIUtility.singleLineHeight + 2);
            property.isExpanded = EditorGUI.Foldout(foldoutPosition, property.isExpanded, label, true, EditorStyles.foldoutHeader);

            // Draw array size on the right
            var intFieldWidth = 76;
            var intFieldPosition = new Rect(position.x + position.width - _indentOffset - intFieldWidth, position.y, _indentOffset + intFieldWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            var size = EditorGUI.DelayedIntField(intFieldPosition, _entriesProperty.arraySize);
            if (EditorGUI.EndChangeCheck())
                _entriesProperty.arraySize = size;

            if (property.isExpanded)
            {
                position.x += _indentOffset;
                position.y += EditorGUIUtility.singleLineHeight + 3;
                position.width -= _indentOffset;
                position.height -= EditorGUIUtility.singleLineHeight;
                _list.DoList(position);
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