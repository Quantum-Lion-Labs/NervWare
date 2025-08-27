#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace NervBox
{
    [CustomEditor(typeof(AudioClipPlayer))]
    public class AudioClipPlayerEditor : UnityEditor.Editor
    {
        private SerializedProperty _audioClipProp;
        private SerializedProperty _loopProp;
        private SerializedProperty _loopSpatialModeProp;

        private SerializedProperty _playOnAwakeProp;
        private SerializedProperty _playOnStartProp;
        private SerializedProperty _playOnEnableProp;
        private SerializedProperty _playOnDisableProp;
        private SerializedProperty _playOnDestroyProp;

        private SerializedProperty _stopOnDisableProp;
        private SerializedProperty _stopOnDestroyProp;

        private SerializedProperty _directivityProp;
        private SerializedProperty _earlyReflectionsSendProp;
        private SerializedProperty _layer1MinDistanceProp;
        private SerializedProperty _volumetricRadiusProp;
        private SerializedProperty _hrtfIntensityProp;
        private SerializedProperty _occlusionIntensityProp;
        private SerializedProperty _reverbReachProp;
        private SerializedProperty _layer1MaxDistanceProp;
        private SerializedProperty _reverbSendProp;

        private GUIStyle _header;
        private GUIStyle _sectionHeader;
        private GUIStyle _foldoutHeader;
        private GUIStyle _contentBox;
        private GUIStyle _toggleButton;
        private GUIStyle _mainBackground;

        private bool _viewAdvancedSettings = false;

        private void OnEnable()
        {
            _audioClipProp = serializedObject.FindProperty("audioClip");
            _loopProp = serializedObject.FindProperty("loop");
            _loopSpatialModeProp = serializedObject.FindProperty("loopSpatialMode");

            _playOnAwakeProp = serializedObject.FindProperty("playOnAwake");
            _playOnStartProp = serializedObject.FindProperty("playOnStart");
            _playOnEnableProp = serializedObject.FindProperty("playOnEnable");
            _playOnDisableProp = serializedObject.FindProperty("playOnDisable");
            _playOnDestroyProp = serializedObject.FindProperty("playOnDestroy");

            _stopOnDisableProp = serializedObject.FindProperty("stopOnDisable");
            _stopOnDestroyProp = serializedObject.FindProperty("stopOnDestroy");


            _directivityProp = serializedObject.FindProperty("directivityIntensity");
            _earlyReflectionsSendProp = serializedObject.FindProperty("earlyReflectionsSend");
            _layer1MinDistanceProp = serializedObject.FindProperty("layer1MinDistance");
            _volumetricRadiusProp = serializedObject.FindProperty("volumetricRadius");
            _hrtfIntensityProp = serializedObject.FindProperty("hrtfIntensity");
            _occlusionIntensityProp = serializedObject.FindProperty("occlusionIntensity");
            _reverbReachProp = serializedObject.FindProperty("reverbReach");
            _layer1MaxDistanceProp = serializedObject.FindProperty("layer1MaxDistance");
            _reverbSendProp = serializedObject.FindProperty("reverbSend");

            InitStyles();
        }


        private void InitStyles()
        {
            _mainBackground = new GUIStyle(EditorStyles.inspectorDefaultMargins)
            {
                normal =
                {
                    background = MakeBackgroundTexture(1, 1, new Color(0.15f, 0.15f, 0.15f, 1.0f))
                },
                padding = new RectOffset(10, 10, 10, 10)
            };

            _header = new GUIStyle(EditorStyles.boldLabel)
            {
                normal =
                {
                    textColor = new Color(0.85f, 0.1f, 0.85f, 1.0f)
                },
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, -2, -2)
            };

            _sectionHeader = new GUIStyle(EditorStyles.boldLabel)
            {
                normal =
                {
                    textColor = new Color(0.1f, 0.7f, 1.0f)
                },
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 0, 0)
            };

            _foldoutHeader = new GUIStyle(EditorStyles.foldoutHeader)
            {
                normal =
                {
                    textColor = _sectionHeader.normal.textColor,
                    background = MakeBackgroundTexture(1, 1, Color.clear)
                },
                onNormal =
                {
                    textColor = _sectionHeader.normal.textColor,
                    background = MakeBackgroundTexture(1, 1, Color.clear)
                },
                hover =
                {
                    background = MakeBackgroundTexture(1, 1, new Color(0.3f, 0.3f, 0.3f, 0.5f))
                },
                onHover =
                {
                    background = MakeBackgroundTexture(1, 1, new Color(0.3f, 0.3f, 0.3f, 0.5f))
                },
                fontSize = _sectionHeader.fontSize,
                fontStyle = _sectionHeader.fontStyle,
                alignment = TextAnchor.MiddleLeft,
                // padding = new RectOffset(15, 5, 5, 5)
            };

            _contentBox = new GUIStyle(EditorStyles.helpBox)
            {
                normal =
                {
                    background = MakeBackgroundTexture(1, 1, new Color(0.25f, 0.25f, 0.25f, 1f)),
                    textColor = Color.white
                },
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 5, 5)
            };

            _toggleButton = new GUIStyle(EditorStyles.toolbarButton)
            {
                normal =
                {
                    background = MakeBackgroundTexture(1, 1, new Color(0.4f, 0.4f, 0.4f, 1.0f)),
                    textColor = Color.white
                },
                onNormal =
                {
                    background = MakeBackgroundTexture(1, 1, new Color(0.2f, 0.6f, 0.2f, 1.0f)),
                    textColor = Color.white
                },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 2, 2),
                margin = new RectOffset(0, 0, 2, 2)
            };
        }

        private Texture2D MakeBackgroundTexture(int width, int height, Color c)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = c;
            }

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical(_mainBackground);

            EditorGUILayout.LabelField("Audio Clip Player", _header);
            EditorGUILayout.Space(15);

            if (!_audioClipProp.objectReferenceValue)
            {
                EditorGUILayout.HelpBox("An AudioClip is required!", MessageType.Error);
            }

            {
                EditorGUILayout.BeginVertical(_contentBox);
                EditorGUILayout.LabelField("Audio Clip Settings", _sectionHeader);
                EditorGUILayout.Space(5);

                EditorGUILayout.PropertyField(_audioClipProp, new GUIContent("Audio Clip"));
                EditorGUILayout.PropertyField(_loopProp, new GUIContent("Loop Audio"));

                if (_loopProp.boolValue)
                {
                    GUI.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
                    EditorGUILayout.PropertyField(_loopSpatialModeProp, new GUIContent("Loop Spatial Mode"));
                    GUI.backgroundColor = Color.white;
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            {
                EditorGUILayout.BeginVertical(_contentBox);
                EditorGUILayout.LabelField("Playback Events", _sectionHeader);
                EditorGUILayout.Space(5);

                DrawToggle(_playOnAwakeProp,
                    "Play On Awake",
                    "Plays the audio when the script is loaded. This is the earliest it will play.");
                DrawToggle(_playOnStartProp,
                    "Play On Start",
                    "Play audio on the first frame update. This is the latest it will play.");
                DrawToggle(_playOnEnableProp,
                    "Play on Enable", "Play when the object is initialized.");
                DrawToggle(_playOnDisableProp,
                    "Play On Disable", "Plays when the object is disabled.");
                DrawToggle(_playOnDestroyProp,
                    "Play On Destroy", "Plays when the object is destroyed");
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            {
                EditorGUILayout.BeginVertical(_contentBox);
                EditorGUILayout.LabelField("Stop Events", _sectionHeader);
                EditorGUILayout.Space(5);
                DrawToggle(_stopOnDisableProp,
                    "Stop On Disable", "Stops the audio when the object is disabled.");
                DrawToggle(_stopOnDestroyProp,
                    "Stop on Destroy", "Stops the audio when the object is destroyed.");
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            {
                if (_loopProp.boolValue && _loopSpatialModeProp.enumValueIndex == 1)
                {
                    //don't draw advanced settings for ambient sounds.
                }
                else
                {
                    EditorGUILayout.BeginVertical(_contentBox);
                    {
                        EditorGUI.indentLevel++;
                        _viewAdvancedSettings =
                            EditorGUILayout.BeginFoldoutHeaderGroup(_viewAdvancedSettings, "Advanced Settings",
                                _foldoutHeader);
                        if (_viewAdvancedSettings)
                        {
                            EditorGUILayout.PropertyField(_directivityProp);
                            EditorGUILayout.PropertyField(_earlyReflectionsSendProp);
                            EditorGUILayout.PropertyField(_layer1MinDistanceProp);
                            EditorGUILayout.PropertyField(_layer1MaxDistanceProp);
                            EditorGUILayout.PropertyField(_volumetricRadiusProp);
                            EditorGUILayout.PropertyField(_hrtfIntensityProp);
                            EditorGUILayout.PropertyField(_occlusionIntensityProp);
                            EditorGUILayout.PropertyField(_reverbReachProp);
                            EditorGUILayout.PropertyField(_reverbSendProp);
                        }

                        EditorGUILayout.EndFoldoutHeaderGroup();
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawToggle(SerializedProperty property, string label, string toolTip)
        {
            GUIContent content = new GUIContent(label, toolTip);
            GUI.backgroundColor = property.boolValue
                ? _toggleButton.onNormal.background.GetPixel(0, 0)
                : _toggleButton.normal.background.GetPixel(0, 0);
            if (GUILayout.Button(content, _toggleButton, GUILayout.Height(25)))
            {
                property.boolValue = !property.boolValue;
            }

            GUI.backgroundColor = Color.white;
        }
    }
}
#endif