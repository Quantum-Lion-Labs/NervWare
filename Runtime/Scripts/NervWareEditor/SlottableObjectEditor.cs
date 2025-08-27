#if UNITY_EDITOR
using NervBox.Editor;
using NervBox.Interaction;
using UnityEditor;
using UnityEngine;

namespace NervWareSDK
{
    [CustomEditor(typeof(SlottableObject))]
    public class SlottableObjectEditor : NervWareStyledEditor
    {
        private SerializedProperty _primaryGrip;
        private SerializedProperty _startPoint;
        private SerializedProperty _endPoint;
        private SerializedProperty _slotType;
        private SerializedProperty _targetPoint;
        private SerializedProperty _flipAxis;
        private SerializedProperty _secondaryAxis;
        private SlottableObject Slottable => target as SlottableObject;
        protected override void InitializeProperties()
        {
            _primaryGrip = serializedObject.FindProperty("primaryGrip");
            _startPoint = serializedObject.FindProperty("startPoint");
            _endPoint = serializedObject.FindProperty("endPoint");
            _slotType = serializedObject.FindProperty("slotType");
            _targetPoint = serializedObject.FindProperty("targetPoint");
            _flipAxis = serializedObject.FindProperty("flipAxis");
            _secondaryAxis = serializedObject.FindProperty("secondaryAxis");
        }

        protected override string GetInspectorName()
        {
            return "SLOTTABLE OBJECT";
        }

        protected override void DrawInspector()
        {
            DrawSection("OBJECT REFERENCES", () =>
            {
                if (_primaryGrip.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("A grip is required!", MessageType.Error);
                }

                EditorGUILayout.PropertyField(_primaryGrip);
                if (_startPoint.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("A start point is required!", MessageType.Error);
                }

                EditorGUILayout.PropertyField(_startPoint);
                if (_endPoint.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("A end point is required!", MessageType.Error);
                }

                EditorGUILayout.PropertyField(_endPoint);
                if (_endPoint.objectReferenceValue == null && _startPoint.objectReferenceValue == null)
                {
                    if (GUILayout.Button("Create Start and End Points"))
                    {
                        Slottable.AddPoints();   
                    }
                }
            });
            
            DrawSection("SETTINGS", () =>
            {
                EditorGUILayout.PropertyField(_slotType);
                EditorGUILayout.PropertyField(_targetPoint);
                EditorGUILayout.PropertyField(_flipAxis);
                string warning = ValidateAxis();
                if (warning != null)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }

                EditorGUILayout.PropertyField(_secondaryAxis);
            });
        }

        private string ValidateAxis()
        {
            return _secondaryAxis.vector3Value == Vector3.zero ? "Axis cannot be zero!" : null;
        }
    }
}
#endif