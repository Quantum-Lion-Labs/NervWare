#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using NervBox.Interaction;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NervWareSDK.Editor
{
    [CustomEditor(typeof(NBImpact))]
    public class NBImpactEditor : UnityEditor.Editor
    {
        private NBImpact _impact;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _impact = target as NBImpact;
            if (_impact == null)
            {
                return;
            }

            if (_impact.gameObject.isStatic)
            {
                DrawStaticEditor();
            }
            else
            {
                DrawDynamicEditor();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static Object ImpactMaterialToMetaXRMaterial(SurfaceType surface)
        {
            const string metaPath = "Packages/com.quantumlionlabs.nervwaresdk/Runtime/ScriptableObjects/MetaXR Materials";
            if (surface == SurfaceType.NoSurface)
            {
                return null;
            }
            var result = AssetDatabase.LoadAssetAtPath<Object>(metaPath + Path.DirectorySeparatorChar + surface + ".asset");
            return result;
        }
        
        private void DrawStaticEditor()
        {
            EditorGUILayout.BeginVertical("window");
            EditorGUILayout.HelpBox("You can use NervBox's materials or define a custom one.",
                MessageType.Info);
            GUIStyle header = new GUIStyle();
            header.fontStyle = FontStyle.Bold;
            header.normal.textColor = Color.cyan;
            GUILayout.Label("Static Settings", header);
            DrawStaticProperties();
            const string geoFullyQualifiedName =
                "MetaXRAcousticGeometry, Meta.XR.Acoustics, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            var geo = _impact.gameObject.GetComponent(Type.GetType(geoFullyQualifiedName));
            var material = _impact.gameObject.GetComponent<MetaXRAcousticMaterial>();
            if ((geo == null || material == null) && GUILayout.Button("Setup Meta XR Material"))
            {
                if (material == null)
                {
                    Debug.Log("no material");
                    material = _impact.gameObject.AddComponent<MetaXRAcousticMaterial>();
                    var field = material.GetType().GetField("properties", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field != null)
                    {
                        Debug.Log("h");
                        field.SetValue(material, ImpactMaterialToMetaXRMaterial(_impact.SurfaceTypeOverride));
                    }
                }

                if (geo == null)
                {
                    geo = _impact.gameObject.AddComponent(Type.GetType(geoFullyQualifiedName));
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private bool _foldoutOpen = true;

        private void DrawDynamicEditor()
        {
            EditorGUILayout.BeginVertical("window");
            EditorGUILayout.HelpBox("You can use NervBox's materials or define a custom one.",
                MessageType.Info);
            GUIStyle header = new GUIStyle();
            header.fontStyle = FontStyle.Bold;
            header.normal.textColor = Color.magenta;
            GUILayout.Label("Dynamic Settings", header);
            DrawStaticProperties();
            if (_impact.gameObject.GetComponentsInChildren<NBImpact>().Length > 1)
            {
                //multi material object
                serializedObject.FindProperty("isMultiMaterialObject").boolValue = true;
            }

            EditorGUI.indentLevel++;
            var hardProp = serializedObject.FindProperty("hardImpactClips");
            EditorGUILayout.PropertyField(hardProp);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("softImpactClips"), true);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private void DrawStaticProperties()
        {
            var overrideSurfaceType = serializedObject.FindProperty("overrideSurfaceType");
            var surfaceType = serializedObject.FindProperty("surfaceTypeOverride");
            var useCustomProperties = serializedObject.FindProperty("useCustomProperties");
            var properties = serializedObject.FindProperty("properties");
            var overrideSurfaceHardness = serializedObject.FindProperty("overrideSurfaceHardness");
            if (!useCustomProperties.boolValue)
            {
                var material =
                    (SurfaceType)EditorGUILayout.EnumPopup("Material Type", (SurfaceType)surfaceType.boxedValue);
                surfaceType.boxedValue = material;
                overrideSurfaceType.boolValue = true;
                overrideSurfaceHardness.boolValue = false;
            }
            else
            {
                overrideSurfaceType.boolValue = false;
                overrideSurfaceHardness.boolValue = false;
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(properties);
                EditorGUI.indentLevel--;
            }

            useCustomProperties.boolValue =
                EditorGUILayout.Toggle("Use Custom Material", useCustomProperties.boolValue);
        }
    }
}
#endif