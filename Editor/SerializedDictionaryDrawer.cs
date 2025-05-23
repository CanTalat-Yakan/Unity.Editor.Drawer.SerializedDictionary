#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Provides a custom property drawer for <see cref="SerializedDictionary{TKey, TValue}"/> types in the Unity
    /// Inspector.
    /// </summary>
    /// <remarks>This drawer enhances the display and editing experience for serialized dictionaries in the
    /// Unity Inspector by: <list type="bullet"> <item>Rendering dictionary entries as a reorderable list.</item>
    /// <item>Allowing customization of the key-value split weight via the <see
    /// cref="KeyValueSplitWeightAttribute"/>.</item> <item>Supporting nested and complex property types for both keys
    /// and values.</item> </list> The drawer automatically handles property initialization and ensures that dictionary
    /// entries are displayed and edited correctly.</remarks>
    [CustomPropertyDrawer(typeof(SerializedDictionary<,>))]
    public class SerializeDictionaryDrawer : PropertyDrawer
    {
        private ReorderableList _list;
        private SerializedProperty _entriesProperty;
        private float _keyWeight = 1;

        private float _indentOffset => 16 * EditorGUI.indentLevel;

        /// <summary>
        /// Initializes the internal state and configuration of the list based on the provided serialized property.
        /// </summary>
        /// <remarks>This method sets up a reorderable list for the provided serialized property,
        /// including callbacks for drawing elements and determining element height. If the list has already been
        /// initialized, the method returns immediately without performing any further actions.</remarks>
        private void Initialize(SerializedProperty property)
        {
            if (_list != null)
                return;

            InspectorHookUtilities.TryGetAttribute<KeyValueSplitWeightAttribute>(property, out var attribute);
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

        /// <summary>
        /// Renders a single element in a custom inspector list.
        /// </summary>
        /// <remarks>This method is typically used for <see cref="ReorderableList"/> to render individual elements
        /// of a serialized property array. It adjusts the layout dynamically based on the weights of the key and value
        /// fields, ensuring proper spacing and alignment.</remarks>
        /// <param name="position">The rectangle on the screen where the element should be drawn.</param>
        /// <param name="index">The zero-based index of the element being drawn.</param>
        /// <param name="isActive">A value indicating whether the element is currently active (e.g., selected).</param>
        /// <param name="isFocused">A value indicating whether the element currently has focus.</param>
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

        /// <summary>
        /// Calculates the height of a UI element at the specified index, based on its key and value properties.
        /// </summary>
        /// <remarks> This method is typically used for <see cref="ReorderableList"/> to determine the height of individual elements
        /// in a serialized property array. It calculates the required height by evaluating both the key and value
        /// properties, ensuring the element is tall enough to fit the larger of the two, plus spacing for proper alignment.
        /// </remarks>
        /// <param name="index">The zero-based index of the element in the array.</param>
        /// <returns>The height of the element, including spacing, as a <see cref="float"/>.  The height is determined by the
        /// larger of the key or value property heights.</returns>
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

        /// <summary>
        /// Calculates the height of the property field in the inspector, accounting for whether the property is
        /// expanded.
        /// </summary>
        /// <remarks>This method dynamically adjusts the height based on the expanded state of the
        /// property. Ensure that the property is properly initialized before calling this method.</remarks>
        /// <param name="property">The serialized property for which the height is being calculated. Must not be <see langword="null"/>.</param>
        /// <param name="label">The label associated with the property. This is typically displayed in the inspector.</param>
        /// <returns>The height, in pixels, required to render the property field. If the property is expanded, the height
        /// includes the additional space for the list; otherwise, it is a single line height.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Initialize(property);
            return property.isExpanded ? _list.GetHeight() + 21 : EditorGUIUtility.singleLineHeight;
        }

        /// <summary>
        /// Renders a custom GUI for a serialized property, including a foldout header and an adjustable array size
        /// field.
        /// </summary>
        /// <remarks>This method provides a foldout header for the property, allowing the user to expand
        /// or collapse its contents. When expanded, the method renders a reorderable list for the property's array
        /// elements. The array size can also be adjusted using a delayed integer field displayed to the right of the
        /// foldout header.</remarks>
        /// <param name="position">The rectangle on the screen to use for the property GUI.</param>
        /// <param name="property">The serialized property to render in the custom GUI.</param>
        /// <param name="label">The label to display alongside the property in the GUI.</param>
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

        /// <summary>
        /// Calculates the total height required to render a serialized property and its visible child properties in the
        /// Unity Editor.
        /// </summary>
        /// <remarks>This method iterates through all visible child properties of the specified <see
        /// cref="SerializedProperty"/>  and accumulates their heights, including spacing between them. The height
        /// calculation respects Unity's  property rendering conventions, including standard vertical spacing.</remarks>
        /// <param name="property">The <see cref="SerializedProperty"/> for which the height is calculated. This property must be valid and
        /// initialized.</param>
        /// <returns>The total height, in pixels, required to render the property and its visible child properties.  If the
        /// property has no visible children, the height of the property itself is returned.</returns>
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