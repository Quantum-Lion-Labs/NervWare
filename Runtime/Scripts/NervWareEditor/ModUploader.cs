#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ModIO;
using Newtonsoft.Json;
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

        public async Task CreateOrUpdateModPage()
        {
            if (_data.logo == null)
            {
                EditorUtility.DisplayDialog("Mod Page Creation Error", 
                    "You must have a logo for your mod!", "ok");
                return;
            }
            var logo = MakeTempLogo(_data.logo);
            ModProfileDetails details = new ModProfileDetails
            {
                logo = logo,
                name = _data.modName,
                summary = _data.modSummary,
                description = _data.modDescription,
                tags = new[] { _data.modType.ToString() },
                visible = false
            };

            if (_data.modIdCache <= 0)
            {
                Debug.Log("Creating new mod...");
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
                var modProfileCurrent = await ModIOUnityAsync.GetMod((ModId)_data.modIdCache);
                bool visible = modProfileCurrent.value.visible;
                details.visible = visible;
                _data.isPublic = visible;
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
        }

        public async Task Publish()
        {
            if (_data.modIdCache <= 0)
            {
                EditorUtility.DisplayDialog("Validation Error", "The mod has not been uploaded!",
                    "ok");
                return;
            }

            ModProfileDetails details = new ModProfileDetails
            {
                visible = true
            };
            Debug.Log("Publishing mod...");
            details.modId = (ModId?)_data.modIdCache;
            var updateCreate = ModIOUnityAsync.EditModProfile(details);
            int progressId = Progress.Start("Publish Mod");
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
                    $"Publish mod failed: {updateCreate.Result.message} {updateCreate.Result.apiMessage}",
                    "ok");
                _data.progress = 0f;
                _data.progressTitle = "";
                return;
            }

            Debug.Log("Published mod.");
        }

        public async Task Upload()
        {
            //TODO: better validation
            if (_data.modAsset == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "There is no prefab or scene assigned!",
                    "ok");
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

            Dictionary<string, string> metaData = new Dictionary<string, string>();
            metaData.Add("bounds", _data.halfExtents.ToString());
            ModProfileDetails details = new ModProfileDetails
            {
                metadata = JsonConvert.SerializeObject(metaData)
            };
            details.modId = (ModId?)_data.modIdCache;
            await ModIOUnityAsync.EditModProfile(details);
            
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
            _data.isUploaded = true;
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