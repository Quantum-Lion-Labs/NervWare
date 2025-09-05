#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NervWareSDK
{
    public class ThumbnailGenerator : EditorWindow
    {
        private GameObject _modelPrefab;

        private string _outputPath = "Assets/Icons/";

        private Vector3 _cameraRotation = new Vector3(20, 45, 0);
        private float _orthographicSize = 1.5f;
        private bool _autoFitToModel = true;
        private float _paddingPercent = 15f;
        private const float MinCameraDistance = 1f;


        private Vector2Int _textureSize = new Vector2Int(1920, 1080);
        private Vector2Int _previewSize = new Vector2Int(320, 240);

        private const float PrimaryLightIntensity = 1.0f;

        private const float SecondaryLightIntensity = 0.3f;

        // Preview components
        private RenderTexture _previewTexture;
        private Camera _renderCamera;
        private GameObject _previewInstance;
        private Light _primaryLight;
        private Light _secondaryLight;
        private BuiltModData _builtModData;

        public static void ShowWindow(BuiltModData data)
        {
            if (data.modAsset == null)
            {
                return;
            }

            var window = GetWindow<ThumbnailGenerator>("Thumbnail Creator");
            if (data.modAsset is GameObject prefab)
            {
                window._modelPrefab = prefab;
            }

            const string folder = "Assets/Generated Logos";
            var fileName = data.modName + "-Logo" + ".asset";
            var completePath = folder + '/' + fileName;
            window._outputPath = completePath;
            window.minSize = new Vector2(400, 550);
            window._builtModData = data;
            if (window._modelPrefab != null)
            {
                window.SetupModelPreview();
            }
            else
            {
                window.SetupScenePreview();
                EditorApplication.update += window.EditorUpdate;
            }
        }
        

        private void EditorUpdate()
        {
            if (this == null)
            {
                EditorApplication.update -= EditorUpdate;
                return;
            }

            UpdateScenePreview();
        }

        private void OnDisable()
        {
            CleanupPreview();
            if (!_modelPrefab)
            {
                EditorApplication.update -= EditorUpdate;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);

            // === CAMERA SETTINGS SECTION (Collapsible) ===
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.Space(5);

                if (_modelPrefab != null)
                {
                    // Rotation sliders
                    EditorGUILayout.LabelField("Rotation", EditorStyles.miniBoldLabel);
                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("X (Pitch)", GUILayout.Width(60));
                    float newRotX = EditorGUILayout.Slider(_cameraRotation.x, -180f, 180f);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Y (Yaw)", GUILayout.Width(60));
                    float newRotY = EditorGUILayout.Slider(_cameraRotation.y, -180f, 180f);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Z (Roll)", GUILayout.Width(60));
                    float newRotZ = EditorGUILayout.Slider(_cameraRotation.z, -180f, 180f);
                    EditorGUILayout.EndHorizontal();

                    if (EditorGUI.EndChangeCheck())
                    {
                        _cameraRotation = new Vector3(newRotX, newRotY, newRotZ);
                        if (_modelPrefab != null)
                        {
                            UpdateModelPreview();
                        }
                    }
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.LabelField("FOV", GUILayout.Width(60));
                    float newFov = EditorGUILayout.Slider(_renderCamera.fieldOfView, 30f, 90f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _renderCamera.fieldOfView = newFov;
                    }
                }

                EditorGUILayout.Space(5);

                if (_modelPrefab != null)
                {
                    // Auto fit settings
                    EditorGUI.BeginChangeCheck();
                    _autoFitToModel = EditorGUILayout.Toggle("Auto Fit to Model", _autoFitToModel);
                    if (_autoFitToModel)
                    {
                        _paddingPercent = EditorGUILayout.Slider("Padding (%)", _paddingPercent, 0f, 50f);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (_modelPrefab != null)
                        {
                            UpdateModelPreview();
                        }
                    }
                }
            }
            EditorGUILayout.EndVertical();

            UpdateScenePreview();

            EditorGUILayout.Space(5);

            // === PREVIEW SECTION ===
            EditorGUILayout.BeginVertical("box");
            {
                if (_modelPrefab != null)
                {
                    UpdateModelPreview();
                }

                if (_previewTexture != null)
                {
                    // Center the preview
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    Rect previewRect = GUILayoutUtility.GetRect(_previewSize.x, _previewSize.y,
                        GUILayout.ExpandWidth(false));
                    EditorGUI.DrawPreviewTexture(previewRect, _previewTexture);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Create Thumbnail", GUILayout.Height(40)))
            {
                if (_modelPrefab != null)
                {
                    GenerateModelIcon();
                }
                else
                {
                    GenerateSceneIcon();
                }
            }

            EditorGUILayout.Space(5);
        }


        private void SetupModelPreview()
        {
            // Preview camera - renders only specified layers
            GameObject cameraObj = new GameObject("IconPreviewCamera");
            cameraObj.hideFlags = HideFlags.HideAndDontSave;
            _renderCamera = cameraObj.AddComponent<Camera>();

            // Configure camera to render only specific layer
            _renderCamera.clearFlags = CameraClearFlags.SolidColor;
            _renderCamera.backgroundColor = Color.clear;
            _renderCamera.orthographic = true;
            _renderCamera.nearClipPlane = 0.1f;
            _renderCamera.farClipPlane = 10f;
            _renderCamera.cullingMask = 1 << 31; // Render only layer 31 (isolated)

            // Lighting setup
            GameObject lightObj = new GameObject("IconPrimaryLight");
            lightObj.hideFlags = HideFlags.HideAndDontSave;
            lightObj.layer = 31; // Place light on isolated layer
            _primaryLight = lightObj.AddComponent<Light>();
            _primaryLight.type = LightType.Directional;
            _primaryLight.color = Color.white;
            _primaryLight.intensity = PrimaryLightIntensity;
            _primaryLight.transform.parent = _renderCamera.transform;
            _primaryLight.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _primaryLight.transform.rotation = Quaternion.Euler(-30, 30, -30);
            _primaryLight.cullingMask = 1 << 31; // Illuminate only layer 31

            GameObject secondaryLightObj = new GameObject("IconSecondaryLight");
            secondaryLightObj.hideFlags = HideFlags.HideAndDontSave;
            secondaryLightObj.layer = 31;
            _secondaryLight = secondaryLightObj.AddComponent<Light>();
            _secondaryLight.type = LightType.Directional;
            _secondaryLight.color = Color.white;
            _secondaryLight.intensity = SecondaryLightIntensity;
            _secondaryLight.transform.parent = _renderCamera.transform;
            _secondaryLight.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _secondaryLight.transform.rotation = Quaternion.Euler(-30, 20, -30);
            _secondaryLight.cullingMask = 1 << 31;

            // RenderTexture for preview
            _previewTexture = new RenderTexture(_previewSize.x, _previewSize.y, 24, RenderTextureFormat.ARGB32);
            _previewTexture.antiAliasing = 4;
            _renderCamera.targetTexture = _previewTexture;
        }

        private void SetupScenePreview()
        {
            GameObject cameraObj = new GameObject("IconPreviewCamera");
            cameraObj.hideFlags = HideFlags.HideAndDontSave;
            _renderCamera = cameraObj.AddComponent<Camera>();

            _renderCamera.clearFlags = CameraClearFlags.Skybox;
            _renderCamera.backgroundColor = Color.clear;
            _renderCamera.useOcclusionCulling = false;

            // RenderTexture for preview
            _previewTexture = new RenderTexture(_previewSize.x, _previewSize.y, 24, RenderTextureFormat.ARGB32);
            _previewTexture.antiAliasing = 4;
            _renderCamera.targetTexture = _previewTexture;
        }

        private void CleanupPreview()
        {
            if (_renderCamera != null) DestroyImmediate(_renderCamera.gameObject);
            if (_primaryLight != null) DestroyImmediate(_primaryLight.gameObject);
            if (_secondaryLight != null) DestroyImmediate(_secondaryLight.gameObject);
            if (_previewInstance != null) DestroyImmediate(_previewInstance);
            if (_previewTexture != null)
            {
                _previewTexture.Release();
                DestroyImmediate(_previewTexture);
            }
        }

        private void UpdateModelPreview()
        {
            if (_renderCamera == null) return;

            // Remove old instance
            if (_previewInstance != null) DestroyImmediate(_previewInstance);

            // Create new model instance
            _previewInstance = Instantiate(_modelPrefab);
            _previewInstance.hideFlags = HideFlags.HideAndDontSave;

            // Place model on isolated layer
            SetLayerRecursively(_previewInstance, 31);

            // Position camera with automatic distance adjustment
            PositionCameraWithDistance();

            // Update camera settings
            _renderCamera.orthographicSize = _orthographicSize;
            _renderCamera.backgroundColor = Color.clear;

            // Auto-fit to model if enabled
            if (_autoFitToModel)
            {
                AutoFitCamera();
            }

            // Render
            _renderCamera.Render();
        }

        private void UpdateScenePreview()
        {
            if (_renderCamera == null) return;
            _renderCamera.gameObject.transform.position = SceneView.lastActiveSceneView.camera.transform.position;
            _renderCamera.gameObject.transform.rotation = SceneView.lastActiveSceneView.camera.transform.rotation;
            _renderCamera.Render();
            Repaint();
        }

        private void PositionCameraWithDistance()
        {
            if (_previewInstance == null) return;

            // Get model bounds for distance calculation
            Renderer[] renderers = _previewInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] is LineRenderer) continue;
                bounds.Encapsulate(renderers[i].bounds);
            }

            // Calculate safe distance based on model size
            float modelSize = bounds.size.magnitude;
            float safeDistance = Mathf.Max(MinCameraDistance, modelSize * 1.5f);

            // Apply rotation
            _renderCamera.transform.rotation = Quaternion.Euler(_cameraRotation);

            // Position camera at safe distance from model center, using default isometric position
            Vector3 modelCenter = bounds.center;
            Vector3 defaultDirection = new Vector3(1, -1, 1).normalized; // Default isometric direction
            Vector3 cameraDirection = _renderCamera.transform.forward.magnitude > 0.1f
                ? _renderCamera.transform.forward
                : defaultDirection;
            _renderCamera.transform.position = modelCenter - cameraDirection * safeDistance;
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void AutoFitCamera()
        {
            if (_previewInstance == null) return;

            // Get model bounds
            var renderers = _previewInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] is LineRenderer) continue;
                bounds.Encapsulate(renderers[i].bounds);
            }

            if (bounds.size.magnitude == 0) return;

            var cameraRight = _renderCamera.transform.right;
            var cameraUp = _renderCamera.transform.up;

            // Project bounds onto camera plane
            var projectedWidth = Mathf.Abs(Vector3.Dot(bounds.size, cameraRight));
            var projectedHeight = Mathf.Abs(Vector3.Dot(bounds.size, cameraUp));

            var maxProjectedSize = Mathf.Max(projectedWidth, projectedHeight);

            // Add padding
            var padding = maxProjectedSize * (_paddingPercent / 100f);
            _orthographicSize = (maxProjectedSize + padding) * 0.5f;
            _renderCamera.orthographicSize = _orthographicSize;

            // Center model in frame
            var center = bounds.center;
            var offset = _renderCamera.transform.position - center;
            var distance = Vector3.Dot(offset, _renderCamera.transform.forward);

            _renderCamera.transform.position = center + _renderCamera.transform.forward * distance;
        }

        private void GenerateSceneIcon()
        {
            // Temporary objects for high-quality rendering
            GameObject tempCameraObj = new GameObject("TempIconCamera");
            Camera tempCamera = tempCameraObj.AddComponent<Camera>();

            try
            {
                tempCamera.clearFlags = CameraClearFlags.Skybox;
                tempCamera.backgroundColor = Color.clear;
                tempCamera.useOcclusionCulling = false;

                tempCameraObj.transform.position = _renderCamera.gameObject.transform.position;
                tempCameraObj.transform.rotation = _renderCamera.gameObject.transform.rotation;
                tempCamera.fieldOfView = _renderCamera.fieldOfView;

                // Create high-quality RenderTexture
                RenderTexture renderTexture = new RenderTexture(_textureSize.x, _textureSize.y, 24,
                    RenderTextureFormat.ARGB32);
                renderTexture.antiAliasing = 8; // Maximum anti-aliasing

                tempCamera.targetTexture = renderTexture;
                tempCamera.Render();

                // Save as PNG
                RenderTexture.active = renderTexture;
                Texture2D texture2D = new Texture2D(_textureSize.x, _textureSize.y, TextureFormat.ARGB32, false);
                texture2D.ReadPixels(new Rect(0, 0, _textureSize.x, _textureSize.y), 0, 0);
                texture2D.Apply();

                const string folder = "Assets/Generated Logos";
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                AssetDatabase.CreateAsset(texture2D, _outputPath);
                var previewAsset = AssetDatabase.LoadAssetAtPath(_outputPath, typeof(Texture2D));
                _builtModData.logo = (Texture2D)EditorUtility.InstanceIDToObject(previewAsset.GetInstanceID());
                // Free memory - clear camera target first to avoid error
                tempCamera.targetTexture = null;
                RenderTexture.active = null;
                renderTexture.Release();
                DestroyImmediate(renderTexture);

                // Update AssetDatabase
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to create icon: {e.Message}", "OK");
            }
            finally
            {
                // Cleanup
                DestroyImmediate(tempCameraObj);
            }
        }


        private void GenerateModelIcon()
        {
            if (_modelPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Select a 3D model to generate icon", "OK");
                return;
            }

            // Temporary objects for high-quality rendering
            GameObject tempCameraObj = new GameObject("TempIconCamera");
            Camera tempCamera = tempCameraObj.AddComponent<Camera>();

            GameObject tempLightObj = new GameObject("TempIconLight");
            Light tempPrimaryLight = tempLightObj.AddComponent<Light>();

            GameObject tempSecondaryLightObj = new GameObject("TempIconSecondaryLight");
            Light tempSecondaryLight = tempSecondaryLightObj.AddComponent<Light>();

            GameObject tempModel = Instantiate(_modelPrefab);

            try
            {
                // Place everything on isolated layer
                tempCameraObj.layer = 31;
                tempLightObj.layer = 31;
                tempSecondaryLightObj.layer = 31;
                SetLayerRecursively(tempModel, 31);

                // Camera setup - render only layer 31
                tempCamera.clearFlags = CameraClearFlags.SolidColor;
                tempCamera.backgroundColor = Color.clear;
                tempCamera.orthographic = true;
                tempCamera.orthographicSize = _orthographicSize;
                tempCamera.nearClipPlane = 0.1f;
                tempCamera.farClipPlane = 10f;
                tempCamera.cullingMask = 1 << 31; // Render only layer 31

                // Position camera with safe distance
                PositionTempCameraWithDistance(tempModel, tempCamera);

                // Lighting setup
                tempPrimaryLight.type = LightType.Directional;
                tempPrimaryLight.color = Color.white;
                tempPrimaryLight.intensity = PrimaryLightIntensity;
                tempPrimaryLight.transform.parent = tempCamera.transform;
                tempPrimaryLight.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                tempPrimaryLight.transform.rotation = Quaternion.Euler(-30, 30, -30);
                tempPrimaryLight.cullingMask = 1 << 31; // Illuminate only layer 31

                tempSecondaryLight.type = LightType.Directional;
                tempSecondaryLight.color = Color.white;
                tempSecondaryLight.intensity = SecondaryLightIntensity;
                tempSecondaryLight.transform.rotation = Quaternion.Euler(30, -20, 30);
                tempSecondaryLight.transform.parent = tempCamera.transform;
                tempSecondaryLight.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                tempSecondaryLight.cullingMask = 1 << 31;

                // Auto-fit if enabled
                if (_autoFitToModel)
                {
                    AutoFitCameraForModel(tempModel, tempCamera);
                }

                // Create high-quality RenderTexture
                RenderTexture renderTexture = new RenderTexture(_textureSize.x, _textureSize.y, 24,
                    RenderTextureFormat.ARGB32);
                renderTexture.antiAliasing = 8; // Maximum anti-aliasing

                tempCamera.targetTexture = renderTexture;
                tempCamera.Render();

                // Save as PNG
                RenderTexture.active = renderTexture;
                Texture2D texture2D = new Texture2D(_textureSize.x, _textureSize.y, TextureFormat.ARGB32, false);
                texture2D.ReadPixels(new Rect(0, 0, _textureSize.x, _textureSize.y), 0, 0);
                texture2D.Apply();

                const string folder = "Assets/Generated Logos";
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                AssetDatabase.CreateAsset(texture2D, _outputPath);
                var previewAsset = AssetDatabase.LoadAssetAtPath(_outputPath, typeof(Texture2D));
                _builtModData.logo = (Texture2D)EditorUtility.InstanceIDToObject(previewAsset.GetInstanceID());
                // Free memory - clear camera target first to avoid error
                tempCamera.targetTexture = null;
                RenderTexture.active = null;
                renderTexture.Release();
                DestroyImmediate(renderTexture);

                // Update AssetDatabase
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to create icon: {e.Message}", "OK");
            }
            finally
            {
                // Cleanup
                DestroyImmediate(tempModel);
                DestroyImmediate(tempCameraObj);
                DestroyImmediate(tempLightObj);
                DestroyImmediate(tempSecondaryLightObj);
            }
        }

        private void PositionTempCameraWithDistance(GameObject model, Camera camera)
        {
            // Get model bounds for distance calculation
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] is LineRenderer) continue;
                bounds.Encapsulate(renderers[i].bounds);
            }

            // Calculate safe distance based on model size
            float modelSize = bounds.size.magnitude;
            float safeDistance = Mathf.Max(MinCameraDistance, modelSize * 1.5f);

            // Apply rotation
            camera.transform.rotation = Quaternion.Euler(_cameraRotation);

            // Position camera at safe distance from model center
            Vector3 modelCenter = bounds.center;
            Vector3 defaultDirection = new Vector3(1, -1, 1).normalized; // Default isometric direction
            Vector3 cameraDirection =
                camera.transform.forward.magnitude > 0.1f ? camera.transform.forward : defaultDirection;
            camera.transform.position = modelCenter - cameraDirection * safeDistance;
        }

        private void AutoFitCameraForModel(GameObject model, Camera camera)
        {
            // Ensure model is on correct layer
            SetLayerRecursively(model, 31);

            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] is LineRenderer) continue;
                bounds.Encapsulate(renderers[i].bounds);
            }

            if (bounds.size.magnitude == 0) return;

            var cameraRight = camera.transform.right;
            var cameraUp = camera.transform.up;

            var projectedWidth = Mathf.Abs(Vector3.Dot(bounds.size, cameraRight));
            var projectedHeight = Mathf.Abs(Vector3.Dot(bounds.size, cameraUp));

            var maxProjectedSize = Mathf.Max(projectedWidth, projectedHeight);
            var padding = maxProjectedSize * (_paddingPercent / 100f);

            camera.orthographicSize = (maxProjectedSize + padding) * 0.5f;

            var center = bounds.center;
            var offset = camera.transform.position - center;
            var distance = Vector3.Dot(offset, camera.transform.forward);

            camera.transform.position = center + camera.transform.forward * distance;
        }
    }
}
#endif