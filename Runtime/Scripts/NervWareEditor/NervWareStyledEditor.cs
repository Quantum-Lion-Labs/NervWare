#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NervWareSDK;
using UnityEditor;
using UnityEngine;

namespace NervBox.Editor
{
    
    public abstract class NervWareStyledEditor : UnityEditor.Editor
    {
        private Texture2D _backgroundColor;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _helpTextStyle;
        private GUIStyle _darkBoxStyle;
        private Dictionary<string, List<SerializedProperty>> _groupProperties = new();

        protected abstract void InitializeProperties();
        protected abstract string GetInspectorName();
        protected abstract void DrawInspector();
        private void OnEnable()
        {
            InitializeProperties();
            _backgroundColor = new Texture2D(1, 1);
            _backgroundColor.SetPixel(0, 0, new Color(0.18f, 0.18f, 0.18f));
            _backgroundColor.Apply();
            foreach (var fieldInfo in target.GetType().GetFields())
            {
                FieldGroupAttribute attribute =
                    fieldInfo.GetCustomAttribute<FieldGroupAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                _groupProperties.TryAdd(attribute.GroupName, new List<SerializedProperty>());
                _groupProperties[attribute.GroupName].Add(serializedObject.FindProperty(fieldInfo.Name));
            }
        }
        
        private void OnDisable()
        {
            DestroyImmediate(_backgroundColor);
            _groupProperties.Clear();
        }

        protected void DrawDivider()
        {
            GUILayout.Space(4);
            Rect rect = EditorGUILayout.GetControlRect(false, 2);
            rect.height = 2;
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 1.0f));
            GUILayout.Space(4);
        }
        
        protected void DrawSection(string title, Action context)
        {
            EditorGUILayout.LabelField(title, _sectionHeaderStyle);
            GUILayout.Space(4);
            EditorGUILayout.BeginVertical(_darkBoxStyle);
            context.Invoke();
            EditorGUILayout.EndVertical();
            GUILayout.Space(20);
        }

        protected void DrawHint(string msg)
        {
            EditorGUILayout.LabelField("You can have stab colliders, slash colliders, or both", _helpTextStyle);
            EditorGUILayout.Space(10);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _helpTextStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                fontSize = 11,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };

            _sectionHeaderStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            _darkBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _backgroundColor },
                padding = new RectOffset(10, 10, 10, 10)
            };

            var screenRect = GUILayoutUtility.GetRect(1, 1);
            var vertRect = EditorGUILayout.BeginVertical();
            Color globalBackgroundColor = new Color(0.07f, 0.07f, 0.07f);
            EditorGUI.DrawRect(
                new Rect(screenRect.x - 13, screenRect.y - 1, screenRect.width + 17, vertRect.height + 9),
                globalBackgroundColor);
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField(GetInspectorName(), headerStyle, GUILayout.Height(25));
                Rect lineRect = EditorGUILayout.GetControlRect(false, 2);
                EditorGUI.DrawRect(lineRect, new Color(0.8f, 0.2f, 0.2f));
                EditorGUILayout.Space(20);
            }
            
            DrawInspector();
            foreach (var (group, properties) in _groupProperties)
            {
                DrawSection(group, () =>
                {
                    foreach (var serializedProperty in properties)
                    {
                        EditorGUILayout.PropertyField(serializedProperty);
                    }
                });
            }
            
            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif