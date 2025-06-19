#if UNITY_EDITOR
using NervBox.Interaction;
using UnityEditor;
using UnityEngine;

namespace NervWareSDK.Editor
{
    //temp
    [CustomPropertyDrawer(typeof(NBImpactProperties))]
    public class ScriptableObjectDrawer : PropertyDrawer
    {
        private UnityEditor.Editor _editor = null;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);
            if (property.objectReferenceValue != null)
            {
                property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, GUIContent.none);
            }

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                if (!_editor)
                {
                    UnityEditor.Editor.CreateCachedEditor(property.objectReferenceValue, null, ref _editor);
                }

                if (_editor)
                {
                    _editor.OnInspectorGUI();
                }

                EditorGUI.indentLevel--;
            }
        }
    }
}
#endif