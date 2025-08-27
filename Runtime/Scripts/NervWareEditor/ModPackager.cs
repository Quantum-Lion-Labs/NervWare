#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ModIO;
using NervBox.Interaction;
using NervBox.SDK;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.SceneManagement;
using UnityEngine;
using Formatting = Newtonsoft.Json.Formatting;
using JsonTextWriter = Newtonsoft.Json.JsonTextWriter;
using JsonWriter = Newtonsoft.Json.JsonWriter;
using Object = UnityEngine.Object;

namespace NervWareSDK.Packaging
{
    public class ModPackager
    {
        private BuiltModData _data;
        private const string LocalLoadPath = "{AddressableVariables.LoadPath}/";
        private const string DefaultGroupName = "Default Local Group";
        private string _acousticPath = "";

        public ModPackager(BuiltModData data)
        {
            _data = data;
        }

        public async Task<bool> PackMod(bool showDialogue)
        {
            if (_data.modAsset == null)
            {
                EditorUtility.DisplayDialog("Mod Failure",
                    "There is no scene or prefab assigned.", "Ok");
                return false;
            }
            var currentTarget = EditorUserBuildSettings.activeBuildTarget;
            var currentGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (_data.modAsset is SceneAsset scene)
            {
                _data.modType = ModType.Map;
                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(scene), OpenSceneMode.Single);
                ValidateSceneLinks();
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }
            else
            {
                //todo: avatar
                GameObject obj = _data.modAsset as GameObject;
                var renderers = obj.GetComponentsInChildren<Renderer>();
                Bounds b = new Bounds(Vector3.zero, Vector3.zero);
                foreach (var renderer in renderers)
                {
                    if (renderer is SkinnedMeshRenderer or MeshRenderer)
                    {
                        b.Encapsulate(renderer.bounds.min);
                        b.Encapsulate(renderer.bounds.max);
                    }
                }

                b.center = Vector3.zero;
                _data.halfExtents = b.extents;
                _data.modType = ModType.Spawnable;
            }

            var buildTask = await BuildForActiveTarget();
            if (!buildTask)
            {
                EditorUtility.DisplayDialog("Mod Failure", "Mod packaged failed.", "Ok");
                return false;
            }

            if (currentTarget == BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,
                    BuildTarget.StandaloneWindows64);
            }
            else
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            buildTask = await BuildForActiveTarget();
            if (!buildTask)
            {
                EditorUtility.DisplayDialog("Mod Failure", "Mod packaged failed.", "Ok");
                return false;
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(currentGroup, currentTarget);
            EditorUtility.SetDirty(_data);
            
            if (showDialogue)
            {
                EditorUtility.DisplayDialog("Mod Success", "Mod packaged successfully.", "Ok");
            }

            return true;
        }

     

        private void ValidateSceneLinks()
        {
            var sceneData = Object.FindAnyObjectByType<ModdedSceneData>(FindObjectsInactive.Exclude);
            if (sceneData == null)
            {
                Debug.Log("No modded scene data found. Adding one!");
                GameObject moddedScene = new GameObject("Modded Scene");
                moddedScene.AddComponent<ModdedSceneData>();
            }

            //destroy audio sources since we have no control of them.
            var audioSources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, 
                FindObjectsSortMode.None);
            foreach (var audioSource in audioSources)
            {
                Object.DestroyImmediate(audioSource);
            }

            var rbs = Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var rigidbody in rbs)
            {
                var netInteractable = rigidbody.GetComponent<NetworkedInteractable>();
                if (!netInteractable)
                {
                    Undo.AddComponent<NetworkedInteractable>(rigidbody.gameObject);
                    EditorUtility.SetDirty(rigidbody);
                }
            }
            
            const string geoFullyQualifiedName =
                "MetaXRAcousticGeometry, Meta.XR.Acoustics, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            const string mapFullyQualifiedName =
                "MetaXRAcousticMap, Meta.XR.Acoustics, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            const string relativePathName = "RelativeFilePath";
            const string relativePathBackupName = "relativeFilePathBackup";
            const string relativePathFieldName = "relativeFilePath";
            const string pathGuidName = "PathGuid";
            var geo = Object.FindObjectsByType(Type.GetType(geoFullyQualifiedName), FindObjectsSortMode.None);
            var map = Object.FindObjectsByType(Type.GetType(mapFullyQualifiedName), FindObjectsSortMode.None);
            if (geo == null || geo.Length == 0)
            {
                Debug.LogWarning("Geo not in the scene, packing without it...");
            }
            else
            {
                int count = 0;
                foreach (var metaGeo in geo)
                {
                    count += ValidateMetaFields(metaGeo);
                    EditorUtility.SetDirty(metaGeo);
                }

                Debug.Log($"Successfully linked {count} geometries(s)!");
            }

            if (map == null || map.Length == 0)
            {
                Debug.LogWarning("Map not in the scene, packing without it...");
                return;
            }

            {
                int count = 0;
                foreach (var metaMap in map)
                {
                    count += ValidateMetaFields(metaMap);
                    EditorUtility.SetDirty(metaMap);
                }
                Debug.Log($"Successfully linked {count} map(s)!");
            }

            _acousticPath = map[0].GetType().GetProperty(relativePathName,
                    BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(map[0]) as string;
            return;

            int ValidateMetaFields(Object metaObject)
            {
                var relativePathProp = metaObject.GetType().GetProperty(relativePathName,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (relativePathProp == null)
                {
                    Debug.LogError($"Could not find property {relativePathName}");
                    return 0;
                }
                var relativePath = relativePathProp.GetValue(metaObject) as string;
                string guid = AssetDatabase.AssetPathToGUID("Assets/" + relativePath);
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogError("Couldn't find GUID for path: " + relativePath);
                    return 0;
                }
                var pathGuid = metaObject.GetType().GetField(pathGuidName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (pathGuid == null)
                {
                    Debug.LogError($"Couldn't find field {pathGuidName}!");
                    return 0;
                }
                pathGuid.SetValue(metaObject, guid);
                Debug.Log($"Set Path GUID {pathGuid.GetValue(metaObject) as string}");
                metaObject.GetType()
                    .GetField(relativePathBackupName,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(metaObject, relativePath);
                // metaObject.GetType()
                //     .GetField(relativePathFieldName,
                //         BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                //     .SetValue(metaObject, guid);
                return 1;
            }
        }
        

        private async Task<bool> BuildForActiveTarget()
        {
            if (string.IsNullOrEmpty(_data.modName))
            {
                Debug.LogWarning("WARNING: No mod name assigned, using the mod asset name instead...");
                _data.modName = _data.modAsset.name;
            }

            var group = AddressableAssetSettingsDefaultObject.Settings.DefaultGroup;
            string assetPath = AssetDatabase.GetAssetPath(_data.modAsset);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);

            if (group == null || guid == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "Unable to find group or guid!",
                    "ok");
                return false;
            }

            string buildPath = Path.Combine(Application.dataPath,
                $"../Mods/{_data.modName}/{EditorUserBuildSettings.activeBuildTarget.ToString()}");

            if (Directory.Exists(buildPath))
            {
                //always want clean build
                Directory.Delete(buildPath, true);
            }

            Directory.CreateDirectory(buildPath);

            foreach (var addressableAssetEntry in group.entries.ToList())
            {
                group.RemoveAssetEntry(addressableAssetEntry);
            }


            var entry = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = guid;
            entry.SetLabel(_data.modType.ToString(), true, true, false);

            AddressableAssetSettingsDefaultObject.Settings.SetDirty(
                AddressableAssetSettings.ModificationEvent.EntryMoved,
                entry, true);

            if (_data.modAsset is SceneAsset && !string.IsNullOrEmpty(_acousticPath))
            {
                //acoustic path is something like Scenes/... so we append Assets to it.
                //assume all audio assets stored in the same folder
                var path = Path.Combine("Assets", _acousticPath);
                path = Path.GetDirectoryName(path);
                Debug.Log(path);
                var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(folder, out string folderGuid, out _);
                Debug.Log(folderGuid);
                var acousticEntry = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(folderGuid, group,
                    false, false);
                acousticEntry.address = folderGuid;
                AddressableAssetSettingsDefaultObject.Settings.SetDirty(
                    AddressableAssetSettings.ModificationEvent.EntryMoved, acousticEntry, true);
            }

            var userTask = ModIOUnityAsync.GetCurrentUser();
            await userTask;
            string userID = userTask.Result.value.userId.ToString();
            string catalogName =
                userID + string.Join("_", _data.modName.Split(Path.GetInvalidFileNameChars()));
            AssetDatabase.SaveAssets();
            AddressableAssetSettingsDefaultObject.Settings.OverridePlayerVersion = catalogName;
            AddressableAssetSettingsDefaultObject.Settings.MonoScriptBundleCustomNaming = catalogName;
            AddressableAssetSettingsDefaultObject.Settings.profileSettings.SetValue(
                AddressableAssetSettingsDefaultObject.Settings.activeProfileId,
                "Local.LoadPath", LocalLoadPath);
            AddressableAssetSettingsDefaultObject.Settings.profileSettings.SetValue(
                AddressableAssetSettingsDefaultObject.Settings.activeProfileId,
                "Local.BuildPath", buildPath);
            AddressableAssetSettingsDefaultObject.Settings.buildSettings.bundleBuildPath = buildPath;
            AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder.ClearCachedData();
            BuildCache.PurgeCache(false);
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            bool success = string.IsNullOrEmpty(result.Error);

            if (!success)
            {
                Debug.LogError("Addressables build error encountered: " + result.Error);
                return false;
            }

            Debug.Log(
                $"<color=green>Successfully build mod for platform: {EditorUserBuildSettings.activeBuildTarget}</color>");

            foreach (var addressableAssetEntry in group.entries.ToList())
            {
                group.RemoveAssetEntry(addressableAssetEntry);
            }

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                _data.androidBuildPath = buildPath;
            }
            else
            {
                _data.windowsBuildPath = buildPath;
            }

            const string infoName = "mod_data.info";
            string infoPath = Path.Combine(buildPath, infoName);
           
            using (StreamWriter file = File.CreateText(infoPath))
            using (JsonWriter writer = new JsonTextWriter(file))
            {
                writer.Formatting = Formatting.Indented;
                await writer.WriteStartObjectAsync();
                await writer.WritePropertyNameAsync("Mod ID");
                await writer.WriteValueAsync(_data.modIdCache);
                await writer.WritePropertyNameAsync("Version");
                await writer.WriteValueAsync(_data.modVersion);
                await writer.WritePropertyNameAsync("SDK Version");
                const string SDKVersion = "0.0.1"; //TODO: dynamic versioning with SDK version
                await writer.WriteValueAsync(SDKVersion);
                await writer.WritePropertyNameAsync("Mod Type");
                await writer.WriteValueAsync(_data.modType.ToString());
                await writer.WriteEndObjectAsync();
            }

            return true;
        }
    }
}
#endif