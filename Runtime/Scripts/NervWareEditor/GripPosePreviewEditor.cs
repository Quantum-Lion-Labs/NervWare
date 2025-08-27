#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NervBox.Editor;
using UnityEditor;
using UnityEngine;
using NervBox.Interaction;
using NervBox.Tools.HandPoser;
using Object = UnityEngine.Object;

namespace NervWareSDK.Editor
{
    [CustomEditor(typeof(NBGripBase), true)]
    public class GripPosePreviewEditor : NervWareStyledEditor
    {
        private NBGripBase _grip = null;
        private bool _previewActive = true;
        private bool _showLeftPreview = true;
        private bool _showRightPreview = true;
        private HandManager _handManager = null;
        private SerializedProperty _poseProperty;
        private SerializedProperty _canBeForcedGrabbed;
        private SerializedProperty _showIndicator;
        private SerializedProperty _grabRotationLimits;
        private SerializedProperty _jointBreakForce;
        private SerializedProperty _holdType;
        private SerializedProperty _requireAdditionalWristMotion;
        private SerializedProperty _ignoreCollideWithBody;
        private SerializedProperty _flipHorizontal;
        private SerializedProperty _allowFlippingUpAxis;
        private SerializedProperty _allowFlippingForwardAxis;
        private SerializedProperty _targetTransform;
        private SerializedProperty _radius;
        private SerializedProperty _height;
        private SerializedProperty _useProceduralPosingFallback;
        private List<HandPose> _handPoses = new List<HandPose>();
        private bool _showAdvancedSettings = false;

        protected override void InitializeProperties()
        {
            _grip = (NBGripBase)target;
            _poseProperty = serializedObject.FindProperty("pose");
            _canBeForcedGrabbed = serializedObject.FindProperty("canBeForceGrabbed");
            _showIndicator = serializedObject.FindProperty("showIndicator");
            _grabRotationLimits = serializedObject.FindProperty("grabRotationLimits");
            _jointBreakForce = serializedObject.FindProperty("jointBreakForce");
            _holdType = serializedObject.FindProperty("holdType");
            _requireAdditionalWristMotion = serializedObject.FindProperty("requireAdditionalWristMotion");
            _ignoreCollideWithBody = serializedObject.FindProperty("<IgnoreCollideWithBody>k__BackingField");
            _flipHorizontal = serializedObject.FindProperty("flipHorizontal");
            _allowFlippingUpAxis = serializedObject.FindProperty("allowFlippingUpAxis");
            _allowFlippingForwardAxis = serializedObject.FindProperty("allowFlippingForwardAxis");
            _targetTransform = serializedObject.FindProperty("targetTransform");
            _radius = serializedObject.FindProperty("radius");
            _height = serializedObject.FindProperty("height");
            _useProceduralPosingFallback = serializedObject.FindProperty("useProceduralPosingFallback");
            GetHandPoses();
        }

        protected override string GetInspectorName()
        {
            var grip = (NBGripBase)target;
            //NOTE (Alec): I understand this routine is redundant and we could just grab type name and toUpper it.
            //I wanted to keep this present in case we rename terminology in the future.
            switch (grip)
            {
                case GenericGrip:
                    return "GENERIC GRIP";
                case PointGrip:
                    return "POINT GRIP";
                case CylinderGrip:
                    return "CYLINDER GRIP";
                case SphereGrip:
                    return "SPHERE GRIP";
            }

            return "GRIP";
        }

        protected override void DrawInspector()
        {
            DrawSection("INTERACTION SETTINGS", () =>
            {
                EditorGUILayout.PropertyField(_canBeForcedGrabbed);
                EditorGUILayout.PropertyField(_showIndicator);
            });

            DrawDivider();

            if (_grip is not GenericGrip)
            {
                DrawPositioningSettings();
            }

            DrawDivider();

            _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings");
            if (_showAdvancedSettings)
            {
                DrawSection("JOINT SETTINGS", () =>
                {
                    EditorGUILayout.PropertyField(_grabRotationLimits);
                    EditorGUILayout.PropertyField(_jointBreakForce);
                });

                DrawSection("INTERACTION SETTINGS", () =>
                {
                    EditorGUILayout.PropertyField(_holdType);
                    EditorGUILayout.PropertyField(_requireAdditionalWristMotion);
                    EditorGUILayout.PropertyField(_ignoreCollideWithBody);
                });

                DrawSection("HAND POSE SETTINGS", () =>
                {
                    _showLeftPreview = GUILayout.Toggle(_showLeftPreview, "Show Left Preview");
                    _showRightPreview = GUILayout.Toggle(_showRightPreview, "Show Right Preview");
                    _previewActive = _showLeftPreview || _showRightPreview;
                    DrawPoseDropDown();
                });
            }
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
        
        private void OnSceneGUI()
        {
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
        }

        private void DrawPositioningSettings()
        {
            DrawSection("POSITIONING SETTINGS", () =>
            {
                EditorGUILayout.PropertyField(_flipHorizontal);
                if (_allowFlippingUpAxis != null && _allowFlippingForwardAxis != null)
                {
                    EditorGUILayout.PropertyField(_allowFlippingUpAxis);
                    EditorGUILayout.PropertyField(_allowFlippingForwardAxis);
                }

                EditorGUILayout.PropertyField(_targetTransform);
                if (_targetTransform.objectReferenceValue == null)
                {
                    if (GUILayout.Button("Create Target Transform"))
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
                        _targetTransform.objectReferenceValue = go;
                    }
                }

                EditorGUILayout.PropertyField(_radius);
                if (_height != null)
                {
                    EditorGUILayout.PropertyField(_height);
                }
            });
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
                EditorGUILayout.PropertyField(_useProceduralPosingFallback);
            }
        }
    }
}
#endif