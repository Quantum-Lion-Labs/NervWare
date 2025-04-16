#if UNITY_EDITOR
using System.IO;
using System.Threading.Tasks;
using ModIO;
using UnityEditor;
using UnityEngine;

namespace NervWareSDK.Packaging
{
    public class ModUploader
    {
        private BuiltModData _data;

        public ModUploader(BuiltModData data)
        {
            _data = data;
        }

        public async Task Upload()
        {
            //TODO: better validation
            if (_data.prefab == null && _data.scene == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "There is no prefab or scene assigned!",
                    "ok");
                return;
            }

            if (_data.logo == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "There is no logo assigned!", "ok");
                return;
            }

            if (_data.logo.width < 512 || _data.logo.height < 288)
            {
                EditorUtility.DisplayDialog("Validation Error",
                    "Mod logo must be at least 512x288", "ok");
                return;
            }

            if (string.IsNullOrEmpty(_data.modName))
            {
                EditorUtility.DisplayDialog("Validation Error", "The mod name is empty!",
                    "ok");
                return;
            }

            if (string.IsNullOrEmpty(_data.androidBuildPath))
            {
                EditorUtility.DisplayDialog("Validation Error", "There is no android path assigned! " +
                                                                "Try re-building.",
                    "ok");
                return;
            }

            if (string.IsNullOrEmpty(_data.windowsBuildPath))
            {
                EditorUtility.DisplayDialog("Validation Error", "There is no windows path assigned!" +
                                                                " Try re-building.",
                    "ok");
                return;
            }

            if (!Directory.Exists(_data.androidBuildPath))
            {
                EditorUtility.DisplayDialog("Validation Error", "There are no mods in the android path! " +
                                                                "Try re-building.",
                    "ok");
                return;
            }

            if (!Directory.Exists(_data.windowsBuildPath))
            {
                EditorUtility.DisplayDialog("Validation Error", "There are no mods in the windows path! " +
                                                                "Try re-building.",
                    "ok");
                return;
            }

            var logo = MakeTempLogo(_data.logo);
            Debug.Log("Mod info valid, beginning upload...");
            Bounds? bounds = null;
            if (_data.prefab != null)
            {
                bounds = GetBounds();
            }

            ModProfileDetails details = new ModProfileDetails
            {
                logo = logo,
                name = _data.modName,
                summary = _data.modSummary,
                description = _data.modDescription,
                tags = new[] { _data.modType.ToString() },
                metadata = bounds.HasValue ? bounds.Value.size.ToString() : "" 
            };

            if (_data.modIdCache <= 0)
            {
                Task<ResultAnd<ModId>> resultCreate =
                    ModIOUnityAsync.CreateModProfile(ModIOUnity.GenerateCreationToken(), details);
                int progressId = Progress.Start("Create Mod Profile");
                while (!resultCreate.IsCompleted)
                {
                    var handle = ModIOUnity.GetCurrentUploadHandle();
                    if (handle != null)
                    {
                        _data.progress = handle.Progress;
                        _data.progressTitle = "Creating Mod Profile";
                        Progress.Report(progressId, handle.Progress);
                    }

                    await Task.Yield();
                }
                Progress.Remove(progressId);

                if (!resultCreate.Result.result.Succeeded())
                {
                    EditorUtility.DisplayDialog("Mod Upload Failed",
                        $"Create Mod Profile failed: {resultCreate.Result.result.message} {resultCreate.Result.result.apiMessage}",
                        "ok");
                    _data.progress = 0f;
                    _data.progressTitle = "";
                    return;
                }

                _data.modIdCache = resultCreate.Result.value;
                Debug.Log("Created Mod Profile");
            }
            else
            {
                Debug.Log("Updating Mod Profile...");
                details.modId = (ModId?)_data.modIdCache;
                var updateCreate = ModIOUnityAsync.EditModProfile(details);
                int progressId = Progress.Start("Update Mod Profile");
                while (!updateCreate.IsCompleted)
                {
                    var handle = ModIOUnity.GetCurrentUploadHandle();
                    if (handle != null)
                    {
                        _data.progress = handle.Progress;
                        _data.progressTitle = "Updating Mod Profile";
                        Progress.Report(progressId, handle.Progress);
                    }

                    await Task.Yield();
                }
                Progress.Remove(progressId);

                if (!updateCreate.Result.Succeeded())
                {
                    EditorUtility.DisplayDialog("Mod Upload Failed",
                        $"Update Mod Profile failed: {updateCreate.Result.message} {updateCreate.Result.apiMessage}",
                        "ok");
                    _data.progress = 0f;
                    _data.progressTitle = "";
                    return;
                }
                Debug.Log("Updated mod profile");
            }

            ModfileDetails windowsFile = new ModfileDetails
            {
                modId = new ModId(_data.modIdCache),
                directory = _data.windowsBuildPath,
                version = _data.modVersion,
                platforms = new string[]
                {
                    GetPlatform(BuildTarget.StandaloneWindows64)
                },
                metadata = _data.modMetaData
            };

            Debug.Log("Uploading Windows File");
            var uploadTask = ModIOUnityAsync.UploadModfile(windowsFile);
            _data.progress = 0.01f;
            int windowsProgress = Progress.Start("Upload Windows Modfile");
            while (!uploadTask.IsCompleted)
            {
                var handle = ModIOUnity.GetCurrentUploadHandle();
                if (handle != null)
                {
                    _data.progress = handle.Progress;
                    _data.progressTitle = "Uploading Windows Build";
                    Progress.Report(windowsProgress, handle.Progress);
                }

                await Task.Yield();
            }
            Progress.Remove(windowsProgress);

            
            if (!uploadTask.Result.Succeeded())
            {
                EditorUtility.DisplayDialog("Mod Upload Failed",
                    $"Windows File upload failure: {uploadTask.Result.message}",
                    "ok");
                _data.progress = 0f;
                _data.progressTitle = "";
                return;
            }
            
            Debug.Log("Uploaded Windows File");

            
            ModfileDetails androidFile = new ModfileDetails
            {
                modId = new ModId(_data.modIdCache),
                directory = _data.androidBuildPath,
                version = _data.modVersion,
                platforms = new string[]
                {
                    GetPlatform(BuildTarget.Android)
                },
                metadata = _data.modMetaData
            };

            Debug.Log("Uploading Android File");
            uploadTask = ModIOUnityAsync.UploadModfile(androidFile);
            _data.progress = 0.01f;
            int androidProgress = Progress.Start("Upload Android Modfile");
            while (!uploadTask.IsCompleted)
            {
                var handle = ModIOUnity.GetCurrentUploadHandle();
                if (handle != null)
                {
                    _data.progress = handle.Progress;
                    _data.progressTitle = "Uploading Android Build";
                    Progress.Report(androidProgress, handle.Progress);
                }

                await Task.Yield();
            }
            Progress.Remove(androidProgress);

            if (!uploadTask.Result.Succeeded())
            {
                EditorUtility.DisplayDialog("Mod Upload Failed",
                    $"Android File upload failure: {uploadTask.Result.message}",
                    "Ok");
                _data.progress = 0f;
                _data.progressTitle = "";
                return;
            }
            Debug.Log("Uploaded Android File");

            EditorUtility.DisplayDialog("Mod Upload Finished",
                "Mod uploaded successfully!",
                "Ok");
            _data.progress = 0f;
            _data.progressTitle = "";
        }

        private static Texture2D MakeTempLogo(Texture2D texture)
        {
            RenderTexture renderTexture =
                RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default,
                    RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, renderTexture);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D readable = new Texture2D(texture.width, texture.height);
            readable.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            readable.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            return readable;
        }
        
        private Bounds GetBounds()
        {
            if (_data.prefab == null)
            {
                return new Bounds();
            }
            Bounds bounds = new Bounds();
            var rot = _data.prefab.transform.rotation;
            _data.prefab.transform.rotation = Quaternion.identity;
            var colliders = _data.prefab.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                if (collider == null) continue;
                if (!collider.enabled || !collider.gameObject.activeSelf || collider.isTrigger) continue;
                bounds.Encapsulate(collider.bounds);
            }
            _data.prefab.transform.rotation = rot;
            return bounds;
        }
        
        
        private static string GetPlatform(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.EmbeddedLinux:
                case BuildTarget.NoTarget:
                    return "windows";
                case BuildTarget.Android:
                    return "android";
                case BuildTarget.PS5:
                    return "ps5";
                default:
                    return "";
            }
        }
    }
}
#endif