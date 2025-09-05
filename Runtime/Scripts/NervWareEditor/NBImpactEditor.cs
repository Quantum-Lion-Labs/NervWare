#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using NervBox.Editor;
using NervBox.Interaction;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NervWareSDK.Editor
{
    [CustomEditor(typeof(NBImpact))]
    [CanEditMultipleObjects]
    public class NBImpactEditor : NervWareStyledEditor
    {
        private NBImpact _impact;
        private SerializedProperty _overrideSurfaceType;
        private SerializedProperty _surfaceTypeOverride;
        private SerializedProperty _useCustomProperties;
        private SerializedProperty _properties;
        private SerializedProperty _overrideSurfaceHardness;
        private SerializedProperty _impactClips;
        private SerializedProperty _impactVolume;
        protected override void InitializeProperties()
        {
            _overrideSurfaceType = serializedObject.FindProperty("overrideSurfaceType");
            _surfaceTypeOverride = serializedObject.FindProperty("surfaceTypeOverride");
            _useCustomProperties = serializedObject.FindProperty("useCustomProperties");
            _properties = serializedObject.FindProperty("properties");
            _overrideSurfaceHardness = serializedObject.FindProperty("overrideSurfaceHardness");
            _impactClips = serializedObject.FindProperty("impactClips");
            _impactVolume = serializedObject.FindProperty("impactClipVolume");
        }

        protected override string GetInspectorName()
        {
            return "IMPACT PROPERTIES";
        }

        protected override void DrawInspector()
        {
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
        }

        private static Object ImpactMaterialToMetaXRMaterial(SurfaceType surface)
        {
            const string metaPath =
                "Packages/com.quantumlionlabs.nervwaresdk/Runtime/ScriptableObjects/MetaXR Materials";
            if (surface == SurfaceType.NoSurface)
            {
                return null;
            }

            var result =
                AssetDatabase.LoadAssetAtPath<Object>(metaPath + Path.DirectorySeparatorChar + surface + ".asset");
            return result;
        }

        private void DrawStaticEditor()
        {
            DrawSection("STATIC SETUP", () =>
            {
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
                        var field = material.GetType()
                            .GetField("properties", BindingFlags.Instance | BindingFlags.NonPublic);
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
            });
        }

        private bool _foldoutOpen = true;

        private void DrawDynamicEditor()
        {
            DrawSection("DYNAMIC SETUP", () =>
            {
                DrawStaticProperties();
                if (_impact.gameObject.GetComponentsInChildren<NBImpact>().Length > 1)
                {
                    //multi material object
                    serializedObject.FindProperty("isMultiMaterialObject").boolValue = true;
                }
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_impactClips);
                EditorGUI.indentLevel--;
                if (_impactClips.isArray && _impactClips.arraySize > 0)
                {
                    EditorGUILayout.PropertyField(_impactVolume);
                }
            });

        }

        private void DrawStaticProperties()
        {
            if (!_useCustomProperties.boolValue)
            {
                var material =
                    (SurfaceType)EditorGUILayout.EnumPopup("Material Type", (SurfaceType)_surfaceTypeOverride.boxedValue);
                _surfaceTypeOverride.boxedValue = material;
                _overrideSurfaceType.boolValue = true;
                _overrideSurfaceHardness.boolValue = false;
            }
            else
            {
                _overrideSurfaceType.boolValue = false;
                _overrideSurfaceHardness.boolValue = false;
                EditorGUILayout.PropertyField(_properties);
            }

            _useCustomProperties.boolValue =
                EditorGUILayout.Toggle("Use Custom Material", _useCustomProperties.boolValue);
        }
    }
}
#endif