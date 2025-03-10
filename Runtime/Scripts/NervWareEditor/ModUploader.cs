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
            //TODO: re-uploads
            if (_data.prefab == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "There is no prefab assigned!",
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
                EditorUtility.DisplayDialog("Validation Error", "There is no android path assigned!",
                    "ok");
                return;
            }

            if (string.IsNullOrEmpty(_data.windowsBuildPath))
            {
                EditorUtility.DisplayDialog("Validation Error", "There is no windows path assigned!",
                    "ok");
                return;
            }

            if (!Directory.Exists(_data.androidBuildPath))
            {
                EditorUtility.DisplayDialog("Validation Error", "There are no mods in the android path!",
                    "ok");
                return;
            }

            if (!Directory.Exists(_data.windowsBuildPath))
            {
                EditorUtility.DisplayDialog("Validation Error", "There are no mods in the windows path!",
                    "ok");
                return;
            }


            ModProfileDetails details = new ModProfileDetails
            {
                logo = _data.logo,
                name = _data.modName,
                summary = _data.modSummary,
                description = _data.modDescription,
            };

            if (_data.modIdCache <= 0)
            {
                Task<ResultAnd<ModId>> resultCreate =
                    ModIOUnityAsync.CreateModProfile(ModIOUnity.GenerateCreationToken(), details);
                while (!resultCreate.IsCompleted)
                {
                    var handle = ModIOUnity.GetCurrentUploadHandle();
                    if (handle != null)
                    {
                        _data.progress = handle.Progress;
                        _data.progressTitle = "Creating Mod Profile";
                    }
                    await Task.Delay(1000);
                }
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

            var uploadTask = ModIOUnityAsync.UploadModfile(windowsFile);
            while (!uploadTask.IsCompleted)
            {
                var handle = ModIOUnity.GetCurrentUploadHandle();
                if (handle != null)
                {
                    _data.progress = handle.Progress;
                    _data.progressTitle = "Uploading Windows Build";
                }

                await Task.Delay(100);
            }

            if (!uploadTask.Result.Succeeded())
            {
                EditorUtility.DisplayDialog("Mod Upload Failed",
                    $"Windows File upload failure: {uploadTask.Result.message}",
                    "ok");
                _data.progress = 0f;
                _data.progressTitle = "";
                return;
            }

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

            uploadTask = ModIOUnityAsync.UploadModfile(androidFile);
            while (!uploadTask.IsCompleted)
            {
                var handle = ModIOUnity.GetCurrentUploadHandle();
                if (handle != null)
                {
                    _data.progress = handle.Progress;
                    _data.progressTitle = "Uploading Android Build";
                }

                await Task.Delay(100);
            }

            if (!uploadTask.Result.Succeeded())
            {
                EditorUtility.DisplayDialog("Mod Upload Failed",
                    $"Android File upload failure: {uploadTask.Result.message}",
                    "Ok");
                _data.progress = 0f;
                _data.progressTitle = "";
                return;
            }

            EditorUtility.DisplayDialog("Mod Upload Finished",
                "Mod uploaded successfully!",
                "Ok");
            _data.progress = 0f;
            _data.progressTitle = "";
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