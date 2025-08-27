#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ModIO;
using NervWareSDK.Packaging;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace NervWareSDK
{
    public enum ModType
    {
        Spawnable,
        Map,
        Avatar,
        None
    }

    [CreateAssetMenu(menuName = "ScriptableObjects/Create Mod Data", fileName = "Assets/New Mod Data")]
    public class BuiltModData : ScriptableObject
    {
        public string modName = "My Mod";

        public string modSummary = "My Excellent Mod Summary";
        public string modDescription = "My Excellent Mod Description";
        public string modVersion = "0.0.1";
        [HideInInspector] public ModType modType = ModType.Spawnable;

        [HideInInspector] public string modMetaData = "metaDataTodo";

        public Object modAsset;

        public Texture2D logo;

        public List<string> categoryTags = new List<string>();
        

        [HideInInspector] public string androidBuildPath;

        [HideInInspector] public string windowsBuildPath;

        [HideInInspector] public long modIdCache = -1;


        public float progress;

        [HideInInspector] public string progressTitle = "";

        [HideInInspector] public List<Hash128> lastModHash = new();

        [HideInInspector] public bool isPublic = false;
        [HideInInspector] public bool isUploaded;

        [HideInInspector] public Vector3 halfExtents;
        [ContextMenu("Reset Mod ID")]
        private void ResetModID()
        {
            modIdCache = -1;
        }

        [ContextMenu("Clear Mod Cache")]
        private void ClearModCache()
        {
            lastModHash.Clear();
        }
        
        internal async void TestInNervBoxWindows()
        {
            AssetDatabase.SaveAssets();
            var needsUpdate = NeedsUpdate();

            if (needsUpdate)
            {
                Debug.Log("Hash Mismatch, rebuilding!");
                ModPackager packager = new ModPackager(this);
                var result = await packager.PackMod(true);
                if (!result)
                {
                    lastModHash.Clear();
                    return;
                }
            }
            else
            {
                Debug.Log("No mod changes detected, building will be skipped!");
            }

            //persistent data path + QLL dir + NB + Test Mods + spawnables + mod name
            string nbDir = Path.Combine(Application.persistentDataPath,
                "../../", "Quantum Lion Labs/NervBox/Test Mods", modType + "s", modName);
            nbDir = Path.GetFullPath(nbDir);
            if (Directory.Exists(nbDir))
            {
                Directory.Delete(nbDir, true);
            }

            Directory.CreateDirectory(nbDir);
            CopyDirectory(windowsBuildPath, nbDir);
            EditorUtility.DisplayDialog("Mod Testing", "Mod copied to NervBox directory succesfully!", "ok");
        }

        private bool NeedsUpdate()
        {
            var dependencies = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(modAsset));
            bool needsUpdate = false;
            if (lastModHash.Count != dependencies.Length)
            {
                needsUpdate = true;
                lastModHash.Clear();
                foreach (var dependency in dependencies)
                {
                    lastModHash.Add(AssetDatabase.GetAssetDependencyHash(dependency));
                }

                Debug.Log("count mismatch");
            }
            else
            {
                for (var i = 0; i < dependencies.Length; i++)
                {
                    var hash = AssetDatabase.GetAssetDependencyHash(dependencies[i]);
                    if (hash != lastModHash[i])
                    {
                        Debug.Log($"hash mismatch {hash} - {lastModHash[i]}");
                        needsUpdate = true;
                    }

                    lastModHash[i] = hash;
                }
            }

            return needsUpdate;
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists)
            {
                return;
            }

            var dirs = dir.GetDirectories();
            Directory.CreateDirectory(destDir);
            var files = dir.GetFiles();
            foreach (var fileInfo in files)
            {
                string path = Path.Combine(destDir, fileInfo.Name);
                fileInfo.CopyTo(path);
            }

            foreach (var subDir in dirs)
            {
                string newDir = Path.Combine(destDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDir);
            }
        }

        internal async void BuildAndUploadMod()
        {
            AssetDatabase.SaveAssets();
            if (NeedsUpdate())
            {
                Debug.Log("Hash Mismatch, rebuilding!");
                ModPackager packager = new ModPackager(this);
                var result = await packager.PackMod(false);
                if (!result)
                {
                    lastModHash.Clear();
                    return;
                }
            }
            else
            {
                Debug.Log("No mod changes detected, building will be skipped!");
            }

            try
            {
                EditorApplication.LockReloadAssemblies();
                ModUploader uploader = new ModUploader(this);
                await uploader.Upload();
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        internal async void ViewModPage()
        {
            if (modIdCache < 0) return;
            var mod = await ModIOUnityAsync.GetMod(new ModId(modIdCache));
            if (mod.result.Succeeded())
            {
                var id = mod.value.nameId;
                Application.OpenURL("https://mod.io/g/nervbox/m/" + id);
            }
        }

        internal async void PublishMod()
        {
            if (modIdCache < 0) return;
            if (!EditorUtility.DisplayDialog("Mod Publish Warning",
                    "Warning: By publishing this mod you are confirming that your mod is functional, performant, and confirms to the NervBox modding rules.",
                    "Publish Mod", "Cancel"))
            {
                return;
            }

            ModUploader uploader = new ModUploader(this);
            await uploader.Publish();
            isPublic = true;
        }

        internal string GetProgressTitle(float curValue, float min, float max, string label)
        {
            return progressTitle;
        }

        internal bool ShowProgress()
        {
            return progress > 0;
        }

        internal string ValidateName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Mod name cannot be empty!";
            }

            return null;
        }

        internal string ValidatePrefab(Object obj)
        {
            if (obj == null)
            {
                return "No scene asset or prefab assigned!";
            }

            if (obj is GameObject && PrefabUtility.IsPartOfPrefabAsset(obj))
            {
                return null;
            }

            if (obj is SceneAsset)
            {
                return null;
            }

            return "Prefab is part of a scene or invalid!";
        }

        internal async void GenerateTempLogo()
        {
            var handle = await GenerateLogo(modName, Color.grey, Color.black);
            var folder = "Assets/Generated Logos";
            var fileName = modName + "-Logo" + ".asset";
            var completePath = folder + '/' + fileName;

            //make a copy of the texture into the asset
            var newTexture = new Texture2D(handle.width, handle.height, handle.format, false);
            newTexture.SetPixels32(handle.GetPixels32());
            newTexture.name = folder + '/' + fileName;

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            AssetDatabase.CreateAsset(newTexture, completePath);
            var previewAsset = AssetDatabase.LoadAssetAtPath(completePath, typeof(Texture2D));
            logo = (Texture2D)EditorUtility.InstanceIDToObject(previewAsset.GetInstanceID());
            // var data = handle.EncodeToPNG();
            // var path = Path.Combine(Application.dataPath, "Generated Logos/");
            // if (!Directory.Exists(path))
            // {
            //     Directory.CreateDirectory(path);
            // }
            // await File.WriteAllBytesAsync(path + modName + ".png", data);
            // logo = 
        }

        private Texture2D GetHeaderImage()
        {
            return (Texture2D)EditorUtility.InstanceIDToObject(
                AssetDatabase.LoadAssetAtPath("Packages/com.quantumlionlabs.nervwaresdk/Editor/nervware.png",
                    typeof(Texture2D)).GetInstanceID());
        }

        private static async Task<Texture2D> GenerateLogo(string text, Color backgroundColor, Color textColor)
        {
            string backgroundColorString = ColorUtility.ToHtmlStringRGB(backgroundColor);
            string textColorString = ColorUtility.ToHtmlStringRGB(textColor);

            Debug.Log($"Colors are: {backgroundColorString} and {textColorString} {text}");

            UnityWebRequest request =
                UnityWebRequestTexture.GetTexture(
                    $"https://placehold.co/512x288/{backgroundColorString}/{textColorString}.png?text={text}");
            request.SendWebRequest();

            while (!request.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"GenerateLogo failed: {request.error}");

                return null;
            }

            return DownloadHandlerTexture.GetContent(request);
        }

        internal async void UpdateModPage()
        {
            ModUploader uploader = new ModUploader(this);
            await uploader.CreateOrUpdateModPage();
        }
    }
}
#endif