#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace NervWareSDK.Editor
{
    [CustomEditor(typeof(BuiltModData))]
    public class BuildModDataEditor : UnityEditor.Editor
    {
        private BuiltModData _target;
        private SerializedProperty _modName;
        private SerializedProperty _modSummary;
        private SerializedProperty _modDescription;
        private SerializedProperty _modVersion;
        private SerializedProperty _modAsset;
        private SerializedProperty _logo;
        private SerializedProperty _progress;
        private SerializedProperty _progressTitle;
        private SerializedProperty _categoryTags;
        
        private Texture2D _headerImage;

        private void OnEnable()
        {
            _target = (BuiltModData)target;
            _modName = serializedObject.FindProperty("modName");
            _modSummary = serializedObject.FindProperty("modSummary");
            _modDescription = serializedObject.FindProperty("modDescription");
            _modVersion = serializedObject.FindProperty("modVersion");
            _modAsset = serializedObject.FindProperty("modAsset");
            _logo = serializedObject.FindProperty("logo");
            _progress = serializedObject.FindProperty("progress");
            _progressTitle = serializedObject.FindProperty("progressTitle");
            _categoryTags = serializedObject.FindProperty("categoryTags");

            _headerImage =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Packages/com.quantumlionlabs.nervwaresdk/Editor/nervware.png");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (_headerImage != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(_headerImage, GUILayout.MaxHeight(50), GUILayout.ExpandWidth(false));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }

            //mod header
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Mod Data", EditorStyles.boldLabel);
            var baseColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.magenta;
            EditorGUILayout.HelpBox("Information about your mod goes here.", MessageType.Info);
            GUI.backgroundColor = baseColor;

            //mod name w/validation
            EditorGUILayout.PropertyField(_modName);
            string nameError = _target.ValidateName(_modName.stringValue);
            if (nameError != null)
            {
                EditorGUILayout.HelpBox(nameError, MessageType.Error);
            }

            bool modCreated = _target.modIdCache != -1;
            string visibility = modCreated ? (_target.isPublic ? "Public" : "Private") : "No Mod Page";
            Color visibilityColor = modCreated ? (_target.isPublic ? Color.green : Color.yellow) : Color.red;

            GUIStyle visibilityTextStyle = new GUIStyle(EditorStyles.label);
            visibilityTextStyle.normal.textColor = visibilityColor;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Mod Page Visibility", visibility, visibilityTextStyle);
            if (modCreated)
            {
                EditorGUILayout.TextField("Mod ID", _target.modIdCache.ToString());
            }

            EditorGUILayout.Vector3Field("Mod Bounds", _target.halfExtents);
            EditorGUI.EndDisabledGroup();


            //summary
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(_logo, new GUIContent("Logo"), true);
            Texture2D logoValue = (Texture2D)_logo.objectReferenceValue;
            
            if (logoValue != null)
            {
                GUILayout.Label(logoValue, GUILayout.Width(128), GUILayout.Height(72));
            }
            else
            {
                EditorGUILayout.HelpBox("No Logo is assigned!", MessageType.Error);
            }
            
            
            if (_target.modAsset != null)
            {
                GUILayoutOption[] thumbnailOptions =
                {
                    GUILayout.Height(40),
                    GUILayout.ExpandWidth(true)
                };

                GUI.backgroundColor = new Color(0.36f, 0.25f, 0.42f, 1f);
                if (GUILayout.Button("Open Thumbnail Creator", thumbnailOptions))
                {
                    ThumbnailGenerator.ShowWindow(_target);
                }

                GUI.backgroundColor = Color.white;
            }

            
            EditorGUILayout.LabelField("Mod Summary");
            _modSummary.stringValue = EditorGUILayout.TextArea(_modSummary.stringValue,
                GUILayout.MinHeight(40));

            //description
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Mod Description");
            _modDescription.stringValue =
                EditorGUILayout.TextArea(_modDescription.stringValue, GUILayout.MinHeight(60));

            //category tags
            if (_target.modAsset is GameObject)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Category Tags");
                CategoryTagsUI.DrawCategoryTagsField(_categoryTags);
            }
            
            //versioning
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(_modVersion);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            //mod asset header
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Mod Assets", EditorStyles.boldLabel);
            GUI.backgroundColor = Color.cyan;
            EditorGUILayout.HelpBox("Setup your mod assets here.", MessageType.Info);

            //mod asset box and preview
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_modAsset, new GUIContent("Mod Asset"), true);
            GUI.backgroundColor = baseColor;
            EditorGUILayout.EndHorizontal();
            var modAsset = _modAsset.objectReferenceValue;
            Texture2D assetPreview = null;
            if (modAsset != null)
            {
                assetPreview = AssetPreview.GetAssetPreview(modAsset);
                if (assetPreview != null)
                {
                    GUILayout.Label(assetPreview, GUILayout.Width(128), GUILayout.Height(128));
                }
            }

            string prefabError = _target.ValidatePrefab(modAsset);
            if (prefabError != null)
            {
                EditorGUILayout.HelpBox(prefabError, MessageType.Error);
            }

            

           

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);
            if (_target.ShowProgress())
            {
                string title = _target.GetProgressTitle(_progress.floatValue, 0f, 1f, "");
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), _progress.floatValue, title);
                EditorGUILayout.Space();
            }

            //mod testing buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Test in NervBox - Windows"))
            {
                _target.TestInNervBoxWindows();
            }

            EditorGUI.BeginDisabledGroup(true);
            if (GUILayout.Button(new GUIContent("Test in NervBox - Android", "Coming soon!")))
            {
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            //build and publish buttons
            EditorGUILayout.LabelField("Publishing", EditorStyles.boldLabel);

            //check if the mod page needs updating
            if (modCreated && GUILayout.Button("Update Mod Page Data"))
            {
                _target.UpdateModPage();
            }

            if (modCreated && GUILayout.Button("Build and Upload Mod Content"))
            {
                _target.BuildAndUploadMod();
            }

            if (modCreated && GUILayout.Button("Open Mod Page in Browser"))
            {
                _target.ViewModPage();
            }


            // next step buttons (create mod page, publish)
            GUILayoutOption[] buttonOptions = new GUILayoutOption[]
            {
                GUILayout.Height(40),
                GUILayout.ExpandWidth(true)
            };

            GUI.backgroundColor = Color.red;

            if (!modCreated && GUILayout.Button("Create Mod Page", buttonOptions))
            {
                _target.UpdateModPage();
            }


            //publish button

            EditorGUILayout.Separator();

            GUIContent publishButtonContent = new GUIContent("Publish Mod",
                !_target.isUploaded
                    ? "You must upload mod content before publishing!"
                    : "Publish your mod to make it publicly visible.");

            EditorGUI.BeginDisabledGroup(!_target.isUploaded);
            //check if mod hasn't been published!
            if (modCreated && !_target.isPublic && GUILayout.Button(publishButtonContent, buttonOptions))
            {
                _target.PublishMod();
            }

            EditorGUI.EndDisabledGroup();

            GUI.backgroundColor = baseColor;

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif