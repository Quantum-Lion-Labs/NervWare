#if UNITY_EDITOR
using System.IO;
using System.Linq;
using ModIO;
using NervWareSDK.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;

namespace NervWareSDK.Packaging
{
    public class ModPackager
    {
        private BuiltModData _data;
        private const string LocalLoadPath = "{AddressableVariables.LoadPath}/";
        private const string DefaultGroupName = "Default Local Group";

        public ModPackager(BuiltModData data)
        {
            _data = data;
        }

        public void PackMod()
        {
            var currentTarget = EditorUserBuildSettings.activeBuildTarget;
            var currentGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (!BuildForActiveTarget())
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

            if (!BuildForActiveTarget())
            {
                EditorUtility.DisplayDialog("Mod Failure", "Mod packaged failed.", "Ok");
                return;
            }
            EditorUserBuildSettings.SwitchActiveBuildTarget(currentGroup, currentTarget);
            EditorUtility.SetDirty(_data);
            EditorUtility.DisplayDialog("Mod Success", "Mod packaged successfully.", "Ok");
        }

        private string GetPlayerVersionOverride()
        {
            return string.Join("_", _data.modName.Split(Path.GetInvalidFileNameChars())); 
        }

        private bool BuildForActiveTarget()
        {
            if (_data.prefab == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "There is no prefab assigned!",
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
            var assetPath = AssetDatabase.GetAssetPath(_data.prefab);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (group == null || guid == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "Unable to find group or guid!",
                    "ok");
                return false;
            }

            string buildPath = Path.Combine(Application.dataPath,
                $"../Mods/{_data.modName}/{EditorUserBuildSettings.activeBuildTarget.ToString()}");
            Debug.Log($"<color=orange>{buildPath}</color>");

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
            Debug.Log(AddressableAssetSettingsDefaultObject.Settings);
            var entry = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, group, false, false);
            Debug.Log($"<color=orange>JSON !!{AddressableAssetSettingsDefaultObject.Settings.EnableJsonCatalog}</color>");
            
            entry.address = guid;
            entry.SetLabel(_data.modType.ToString(), true, true, false);
            AddressableAssetSettingsDefaultObject.Settings.SetDirty(
                AddressableAssetSettings.ModificationEvent.EntryMoved,
                entry, true);
            AssetDatabase.SaveAssets();
            AddressableAssetSettingsDefaultObject.Settings.OverridePlayerVersion = string.Join("_", _data.modName.Split(Path.GetInvalidFileNameChars()));
            AddressableAssetSettingsDefaultObject.Settings.profileSettings.SetValue(
                AddressableAssetSettingsDefaultObject.Settings.activeProfileId,
                "Local.LoadPath", LocalLoadPath);
            AddressableAssetSettingsDefaultObject.Settings.profileSettings.SetValue(
                AddressableAssetSettingsDefaultObject.Settings.activeProfileId,
                "Local.BuildPath", buildPath);
            AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder.ClearCachedData();
            BuildCache.PurgeCache(false);
            Debug.Log($"<color=green>{AddressableAssetSettingsDefaultObject.Settings.EnableJsonCatalog}");
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            bool success = string.IsNullOrEmpty(result.Error);

            if (!success)
            {
                Debug.LogError("Addressables build error encountered: " + result.Error);
                return false;
            }
            else
            {
                Debug.Log(
                    $"<color=green>Successfully build mod for platform: {EditorUserBuildSettings.activeBuildTarget}</color>");
            }
            
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