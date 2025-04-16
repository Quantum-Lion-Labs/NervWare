#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.Rendering;

namespace NervWareSDK.Editor
{
    public static class NervWareValidator
    {
        private const string NervWarePackageDirectory =
            "Packages/com.quantumlionlabs.nervwaresdk/Editor/Settings Assets";

        private static readonly string LocalSettingsDirectory =
            Path.Combine(Application.dataPath, "../ProjectSettings");

        public static void ValidateAll()
        {
            if (!EditorUtility.DisplayDialog("Project Injection",
                    "WARNING: This will overwrite all of your project settings. " +
                    "Are you sure you want to continue?",
                    "Yes", "No"))
            {
                return;
            }

            try
            {
                SetupAddressablesSettings();
                SetupProjectPreferences();
                SetupGraphicsSettings();
                SetupURPProjectSettings();
                SetupPlayerSettings();
                SetupTagManager();
                SetupDynamics();
                SetupQualitySettings();
                SetupSaintsField();
                EditorUtility.RequestScriptReload();
                EditorUtility.DisplayDialog("Project Injection",
                    "Injection completed successfully!", "OK");
                AssetDatabase.Refresh();
                EditorPrefs.SetBool("HasValidated", true);
                if (EditorUtility.DisplayDialog("Project Injection", "You now need to restart your project" +
                                                                     " for the NervWare SDK to be fully setup. " +
                                                                     "Would you like to restart now?", "Yes", "No"))
                {
                    EditorApplication.OpenProject(Directory.GetCurrentDirectory());
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Project Validation",
                    $"A problem occured while validating: {e.Message}. Please contact QLL for support.",
                    "OK");
            }
        }

        public static void SetupProjectPreferences()
        {
            var type = typeof(ProjectConfigData).GetProperty("AutoOpenAddressablesReport",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (type != null)
            {
                type.SetValue(null, false);
            }
            else
            {
                Debug.Log("No AutoOpenAddressablesReport found.");
            }
        }

        public static void SetupSaintsField()
        {
            SaintsField.Editor.Utils.SaintsMenu.CreateOrEditSaintsFieldConfig();
        }

        public static void SetupDynamics()
        {
            ReplaceFile("DynamicsManager.asset.txt", "DynamicsManager.asset");
        }

        public static void SetupTagManager()
        {
            ReplaceFile("TagManager.asset.txt", "TagManager.asset");
        }

        public static void SetupURPProjectSettings()
        {
            ReplaceFile("URPProjectSettings.asset.txt", "URPProjectSettings.asset");
        }

        public static void SetupAddressablesSettings()
        {
            if (Directory.Exists(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder))
            {
                Directory.Delete(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, true);
            }

            if (File.Exists(
                    AssetDatabase.GetTextMetaFilePathFromAssetPath(AddressableAssetSettingsDefaultObject
                        .kDefaultConfigFolder)))
            {
                File.Delete(AssetDatabase.GetTextMetaFilePathFromAssetPath(AddressableAssetSettingsDefaultObject
                    .kDefaultConfigFolder));
            }

            Directory.CreateDirectory(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder);

            var settings = AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                "AdddressableAssetSettings", true, true);
            AddressableAssetSettingsDefaultObject.Settings = settings;
            for (int i = settings.groups.Count - 1; i >= 0; i--)
            {
                var group = settings.groups[i];
                if (group == null) continue;
                if (group.Default)
                {
                    foreach (var addressableAssetEntry in group.entries.ToList())
                    {
                        group.RemoveAssetEntry(addressableAssetEntry);
                    }
                }
                else
                {
                    settings.RemoveGroup(group);
                }
            }

            if (Directory.Exists(settings.DataBuilderFolder))
            {
                AssetDatabase.DeleteAsset(settings.DataBuilderFolder);
            }

            if (Directory.Exists(settings.GroupFolder))
            {
                AssetDatabase.DeleteAsset(settings.GroupFolder);
            }

            if (Directory.Exists(settings.GroupTemplateFolder))
            {
                AssetDatabase.DeleteAsset(settings.GroupTemplateFolder);
            }

            PlayerPrefs.DeleteKey(Addressables.kAddressablesRuntimeDataPath);
            Preset addressableSettingsPreset = AssetDatabase.LoadAssetAtPath<Preset>(NervWarePackageDirectory +
                "/AddressableAssetSettings.preset");
            if (addressableSettingsPreset == null)
            {
                throw new FileNotFoundException("AddressableAssetSettings.preset not found");
            }

            addressableSettingsPreset.ApplyTo(settings);
            settings.EnableJsonCatalog = true;
            for (int i = settings.GroupTemplateObjects.Count - 1; i >= 0; i--)
            {
                settings.RemoveGroupTemplateObject(i);
            }

            AddressableAssetSettingsDefaultObject.Settings = settings;
            RemoveMissingGroupReferences();
            settings = AddressableAssetSettingsDefaultObject.Settings;

            UnityEditor.Build.Pipeline.Utilities.ScriptableBuildPipeline.useDetailedBuildLog = true;
            ProjectConfigData.GenerateBuildLayout = true;
            AddressablesRuntimeProperties.ClearCachedPropertyValues();
            AddressableAssetGroupSchema schema =
                AssetDatabase.LoadAssetAtPath<AddressableAssetGroupSchema>(NervWarePackageDirectory +
                                                                           "/Default Local Group_BundledAssetGroupSchema.asset");
            AddressableAssetGroupSchema schema2 =
                AssetDatabase.LoadAssetAtPath<AddressableAssetGroupSchema>(NervWarePackageDirectory +
                                                                           "/Default Local Group_ContentUpdateGroupSchema.asset");
            if (schema == null)
            {
                throw new FileNotFoundException("AddressableAssetGroupSchema.asset not found");
            }

            if (schema2 == null)
            {
                throw new FileNotFoundException("AddressableAssetGroupSchema.asset not found");
            }

            settings.DefaultGroup.ClearSchemas(true);
            settings.DefaultGroup.AddSchema(schema);
            settings.DefaultGroup.AddSchema(schema2);
            settings.EnableJsonCatalog = true;

            string buildPath = Path.Combine(Application.dataPath,
                $"../Mods/");
            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }

            settings.profileSettings.SetValue(settings.activeProfileId, "Local.BuildPath", buildPath);
            settings.profileSettings.SetValue(settings.activeProfileId, "Local.LoadPath",
                "{AddressableVariables.LoadPath}/");
            settings.ActivePlayerDataBuilder.ClearCachedData();
            AddressablesRuntimeProperties.ClearCachedPropertyValues();
            AddressableAssetSettingsDefaultObject.Settings = settings;
            AddressableAwakeForce();
            AssetDatabase.SaveAssets();
        }

        private static bool RemoveMissingGroupReferences()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var groups = settings.groups;
            List<int> missingGroupsIndices = new List<int>();
            for (int i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                if (g == null)
                    missingGroupsIndices.Add(i);
            }

            if (missingGroupsIndices.Count > 0)
            {
                Debug.Log("Addressable settings contains " + missingGroupsIndices.Count +
                          " group reference(s) that are no longer there. Removing reference(s).");
                for (int i = missingGroupsIndices.Count - 1; i >= 0; i--)
                    groups.RemoveAt(missingGroupsIndices[i]);
                AddressableAssetSettingsDefaultObject.Settings = settings;
                return true;
            }

            return false;
        }

        [InitializeOnLoadMethod]
        public static void AddressableAwakeForce()
        {
            if (AddressableAssetSettingsDefaultObject.Settings != null)
            {
                //unity is pretty fucking stupid
                typeof(AddressableAssetSettings).GetMethod("Awake",
                        BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(AddressableAssetSettingsDefaultObject.Settings, null);
            }
        }

        public static void SetupGraphicsSettings()
        {
            string packagePath = Path.GetFullPath(NervWarePackageDirectory + "/GraphicsSettings.asset.txt");
            if (string.IsNullOrEmpty(packagePath) || !File.Exists(packagePath))
            {
                throw new FileNotFoundException($"Unable to find GraphicsSettings package at {packagePath}!");
            }

            string localFilePath = Path.GetFullPath(LocalSettingsDirectory + "/GraphicsSettings.asset");
            if (string.IsNullOrEmpty(localFilePath) || !File.Exists(localFilePath))
            {
                throw new FileNotFoundException($"Unable to find GraphicsSettings local file at {localFilePath}!");
            }

            StreamReader reader = new StreamReader(localFilePath);
            string importPackage = reader.ReadToEnd();
            reader.Close();

            string globalSettingsAssetPath =
                NervWarePackageDirectory + "/UniversalRenderPipelineGlobalSettings.asset.txt.meta";
            string guid = AssetDatabase.AssetPathToGUID(globalSettingsAssetPath);

            importPackage = importPackage.Replace("{NERVBOX_GLOBAL_SETTINGS}", guid);
            File.WriteAllText(localFilePath, importPackage);
            UnityEngine.Rendering.GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var asset);
            var globalSettingsPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(globalSettingsPath))
            {
                throw new FileNotFoundException($"Unable to find global settings asset at {globalSettingsPath}!");
            }

            reader = new StreamReader(globalSettingsPath);
            string globalSettingsContent = reader.ReadToEnd();
            reader.Close();
            globalSettingsContent = globalSettingsContent.Replace("m_ProbeVolumeDisableStreamingAssets: 0",
                "m_ProbeVolumeDisableStreamingAssets: 1");
            File.WriteAllText(globalSettingsPath, globalSettingsContent);
        }

        public static void SetupPlayerSettings()
        {
            string packagePath = Path.GetFullPath(NervWarePackageDirectory + "/ProjectSettings.asset.txt");
            if (string.IsNullOrEmpty(packagePath) || !File.Exists(packagePath))
            {
                throw new FileNotFoundException($"Unable to find ProjectSettings! at {packagePath}");
            }

            string localFilePath = Path.GetFullPath(LocalSettingsDirectory + "/ProjectSettings.asset");
            if (string.IsNullOrEmpty(localFilePath) || !File.Exists(localFilePath))
            {
                throw new FileNotFoundException($"Unable to find local Project Settings! at {localFilePath}");
            }

            StreamReader reader = new StreamReader(packagePath);
            string importPackage = reader.ReadToEnd();
            reader.Close();

            string companyName = PlayerSettings.companyName;
            string productName = PlayerSettings.productName;
            string version = PlayerSettings.bundleVersion;

            importPackage = importPackage.Replace("{COMPANY_REPLACE_ME}", companyName);
            importPackage = importPackage.Replace("{PRODUCT_REPLACE_ME}", productName);
            importPackage = importPackage.Replace("{VERSION_REPLACE_ME}", version);
            File.WriteAllText(localFilePath, importPackage);
        }

        public static void SetupQualitySettings()
        {
            string packagePath = Path.GetFullPath(NervWarePackageDirectory + "/QualitySettings.asset.txt");
            if (string.IsNullOrEmpty(packagePath) || !File.Exists(packagePath))
            {
                throw new FileNotFoundException($"Unable to find Quality Settings! at {packagePath}");
                return;
            }

            string localFilePath = Path.GetFullPath(LocalSettingsDirectory + "/QualitySettings.asset");
            if (string.IsNullOrEmpty(localFilePath) || !File.Exists(localFilePath))
            {
                throw new FileNotFoundException($"Unable to find local Quality Settings! at {localFilePath}");
                return;
            }

            StreamReader reader = new StreamReader(packagePath);
            string importPackage = reader.ReadToEnd();
            reader.Close();

            const string highQualityGuidPath = NervWarePackageDirectory + "/URP-HighFidelity.asset.meta";
            const string mediumQualityGuidPath = NervWarePackageDirectory + "/URP-Balanced.asset.meta";
            const string lowQualityGuidPath = NervWarePackageDirectory + "/URP-Performant.asset.meta";

            var highQualityGuid = ExtractGUIDFromMeta(highQualityGuidPath);
            var mediumQualityGuid = ExtractGUIDFromMeta(mediumQualityGuidPath);
            var lowQualityGuid = ExtractGUIDFromMeta(lowQualityGuidPath);

            importPackage = importPackage.Replace("{NERVBOX_HIGH}", highQualityGuid);
            importPackage = importPackage.Replace("{NERVBOX_MEDIUM}", mediumQualityGuid);
            importPackage = importPackage.Replace("{NERVBOX_LOW}", lowQualityGuid);
            File.WriteAllText(localFilePath, importPackage);
        }

        private static string ExtractGUIDFromMeta(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"No file at {path}!");
            }

            var contents = File.ReadAllLines(path);
            foreach (var line in contents)
            {
                if (line.Trim().StartsWith("guid:"))
                {
                    return line.Trim().Replace("guid:", "");
                }
            }

            return null;
        }

        private static void ReplaceFile(string packageName, string localName)
        {
            string packagePath = Path.GetFullPath(NervWarePackageDirectory + "/" + packageName);
            if (string.IsNullOrEmpty(packagePath) || !File.Exists(packagePath))
            {
                throw new FileNotFoundException($"Unable to find {packageName}! at {packagePath}");
            }

            string localFilePath = Path.GetFullPath(LocalSettingsDirectory + "/" + localName);
            if (string.IsNullOrEmpty(localFilePath) || !File.Exists(localFilePath))
            {
                throw new FileNotFoundException($"Unable to find local {localName}! at {localFilePath}");
            }

            File.Copy(packagePath, localFilePath, true);
        }
    }
}
#endif