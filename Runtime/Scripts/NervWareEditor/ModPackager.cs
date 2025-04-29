#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ModIO;
using NervWareSDK.Editor;
using NervBox.SDK;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
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

        public async void PackMod()
        {
            var currentTarget = EditorUserBuildSettings.activeBuildTarget;
            var currentGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (_data.scene != null)
            {
                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(_data.scene), OpenSceneMode.Single);
                EnsureModSceneData();
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }

            var buildTask = await BuildForActiveTarget();
            if (!buildTask)
            {
                EditorUtility.DisplayDialog("Mod Failure", "Mod packaged failed.", "Ok");
                return;
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
                return;
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(currentGroup, currentTarget);
            EditorUtility.SetDirty(_data);
            EditorUtility.DisplayDialog("Mod Success", "Mod packaged successfully.", "Ok");
        }

        private void EnsureModSceneData()
        {
            var sceneData = Object.FindAnyObjectByType<ModdedSceneData>(FindObjectsInactive.Exclude);
            if (sceneData == null)
            {
                Debug.Log("No modded scene data found. Adding one!");
                GameObject moddedScene = new GameObject("Modded Scene");
                moddedScene.AddComponent<ModdedSceneData>();
            }

            const string GeoFullyQualifiedName =
                "MetaXRAcousticGeometry, Meta.XR.Acoustics, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            var geo = Object.FindAnyObjectByType(Type.GetType(GeoFullyQualifiedName));
            if (geo == null)
            {
                Debug.LogWarning("Geo not in the scene, packing without it...");
                return;
            }

            const string RelativePathName = "RelativeFilePath";
            _acousticPath = geo.GetType().GetProperty(RelativePathName,
                    BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(geo) as string;
        }

        private async Task<bool> BuildForActiveTarget()
        {
            if (_data.prefab == null && _data.scene == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "There is no prefab or scene assigned!",
                    "ok");
                return false;
            }

            if (string.IsNullOrEmpty(_data.modName))
            {
                EditorUtility.DisplayDialog("Validation Error", "The mod name is empty!",
                    "ok");
                return false;
            }

            var group = AddressableAssetSettingsDefaultObject.Settings.DefaultGroup;
            string assetPath = null;
            switch (_data.modType)
            {
                case ModType.Spawnable:
                    assetPath = AssetDatabase.GetAssetPath(_data.prefab);
                    break;
                case ModType.Map:
                    assetPath = AssetDatabase.GetAssetPath(_data.scene);
                    break;
                case ModType.Avatar:
                case ModType.None:
                    EditorUtility.DisplayDialog("Validation Error", "Avatar mods are not supported yet!",
                        "ok");
                    return false;
            }

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
            Debug.Log(guid);
            entry.SetLabel(_data.modType.ToString(), true, true, false);

            AddressableAssetSettingsDefaultObject.Settings.SetDirty(
                AddressableAssetSettings.ModificationEvent.EntryMoved,
                entry, true);

            if (_data.scene != null && !string.IsNullOrEmpty(_acousticPath))
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

            return true;
        }
    }
}
#endif