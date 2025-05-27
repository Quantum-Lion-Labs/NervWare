#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NervBox.Interaction;
using NervBox.Tools.HandPoser;
using Object = UnityEngine.Object;

namespace NervWareSDK.Editor
{
    [CustomEditor(typeof(NBGripBase), true)]
    public class GripPosePreviewEditor : UnityEditor.Editor
    {
        private NBGripBase _grip = null;
        private bool _previewActive = true;
        private bool _showLeftPreview = true;
        private bool _showRightPreview = true;
        private HandManager _handManager = null;
        private SerializedProperty _poseProperty;
        private List<HandPose> _handPoses = new List<HandPose>();
        private bool _showAdvancedSettings = false;

        private void OnEnable()
        {
            _grip = (NBGripBase)target;
            _poseProperty = serializedObject.FindProperty("pose");
            GetHandPoses();
        }

        private void OnDisable()
        {
            if (_handManager != null)
            {
                DestroyImmediate(_handManager.gameObject);
            }

            if (_grip != null)
            {
                _grip.OnDisable();
            }
        }

        private void GetHandPoses()
        {
            _handPoses.Clear();
            var guids = AssetDatabase.FindAssets("t:HandPose");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                HandPose handPose = AssetDatabase.LoadAssetAtPath<HandPose>(path);
                if (handPose != null)
                {
                    _handPoses.Add(handPose);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (Application.isPlaying)
            {
                return;
            }

            serializedObject.Update();
           

         
            
            EditorGUILayout.Separator();
            GUILayout.BeginVertical("Interaction Settings", "window");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("canBeForceGrabbed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showIndicator"));
            
            GUILayout.EndVertical();
          
            EditorGUILayout.Separator();

            GUILayout.BeginVertical("Positioning Settings", "window");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("flipHorizontal"));
            var up = serializedObject.FindProperty("allowFlippingUpAxis");
            var fwd = serializedObject.FindProperty("allowFlippingForwardAxis");
            if (up != null && fwd != null)
            {
                EditorGUILayout.PropertyField(up);
                EditorGUILayout.PropertyField(fwd);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetTransform"));
            if (GUILayout.Button("Create Target Transform"))
            {
                if (serializedObject.FindProperty("targetTransform").objectReferenceValue == null)
                {
                    GameObject go = new GameObject("Target Transform")
                    {
                        transform =
                        {
                            parent = _grip.transform,
                            localPosition = Vector3.zero,
                            localRotation = Quaternion.identity,
                            localScale = Vector3.one
                        }
                    };
                    serializedObject.FindProperty("targetTransform").objectReferenceValue = go;
                }
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("radius"));
            var height = serializedObject.FindProperty("height");
            if (height != null)
            {
                EditorGUILayout.PropertyField(height);
            }

            GUILayout.EndVertical();
            EditorGUILayout.Separator();
            
            _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings");
            if (_showAdvancedSettings)
            {
                EditorGUILayout.Separator();

                GUILayout.BeginVertical("Hand Joint Settings", "window");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("grabRotationLimits"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("jointBreakForce"));
                GUILayout.EndVertical();
                
                EditorGUILayout.Separator();
                GUILayout.BeginVertical("Interaction Settings", "window");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("holdType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("requireAdditionalWristMotion"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("<IgnoreCollideWithBody>k__BackingField"));
                GUILayout.EndVertical();
                
                EditorGUILayout.Separator();
                GUILayout.BeginVertical("Hand Pose Settings", "window");
                _showLeftPreview = GUILayout.Toggle(_showLeftPreview, "Show Left Preview");
                _showRightPreview = GUILayout.Toggle(_showRightPreview, "Show Right Preview");
                _previewActive = _showLeftPreview || _showRightPreview;
                DrawPoseDropDown();
                GUILayout.EndVertical();
            }
            
            GUILayout.FlexibleSpace();


            if (_previewActive && _poseProperty.objectReferenceValue != null)
            {
                if (_handManager == null)
                {
                    GameObject manager = Resources.Load<GameObject>(
                        "Hand Manager");
                    var obj = GameObject.Instantiate(manager);
                    _handManager = obj.GetComponent<HandManager>();
                    _handManager.LeftHand.Toggle(_showLeftPreview);
                    _handManager.RightHand.Toggle(_showRightPreview);
                    _handManager.UpdateHands(_poseProperty.objectReferenceValue as HandPose, _grip, false);
                    _handManager.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    _handManager.LeftHand.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    _handManager.RightHand.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }

                _handManager.LeftHand.Toggle(_showLeftPreview);
                _handManager.RightHand.Toggle(_showRightPreview);
                _handManager.UpdateHands(_poseProperty.objectReferenceValue as HandPose, _grip, false);
            }
            else
            {
                OnDisable();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPoseDropDown()
        {
            EditorGUILayout.LabelField("NOTE: Hand poses are optional!");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_poseProperty);
            if (GUILayout.Button("Choose Pose"))
            {
                List<Object> poses = new();
                _handPoses.ForEach(pose => { poses.Add(pose); });
                AssetPreviewWindow.Show(poses, (o =>
                {
                    _poseProperty.objectReferenceValue = o;
                    _poseProperty.serializedObject.ApplyModifiedProperties();
                }), "Hand Poses");
            }


            EditorGUILayout.EndHorizontal();
            if (_poseProperty.objectReferenceValue == null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useProceduralPosingFallback"));
            }
        }
    }
}
#endif