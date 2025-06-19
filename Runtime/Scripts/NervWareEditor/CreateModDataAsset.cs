#if UNITY_EDITOR
using System.IO;
using ModIO;
using UnityEditor;
using UnityEngine;

namespace NervWareSDK.Editor
{
    public static class CreateModDataAsset
    {
        [MenuItem("Assets/Create Mod Data", true, priority = 1)]
        private static bool CheckAsset()
        {
            return Selection.activeObject != null && (PrefabUtility.IsPartOfAnyPrefab(Selection.activeObject) ||
                                                      Selection.activeObject is SceneAsset);
        }

        [MenuItem("Assets/Create Mod Data", false, priority = 1)]
        private static void CreateAsset()
        {
            var currentObject = Selection.activeObject;
            var name = currentObject.name;
            var assets = AssetDatabase.FindAssets(name + "ModData");
            if (assets.Length > 0)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(assets[0]));
                return;
            }

            BuiltModData newData = ScriptableObject.CreateInstance<BuiltModData>();
            newData.modName = name;
            if (currentObject is GameObject)
            {
                newData.modType = ModType.Spawnable;
            }
            else if (currentObject is SceneAsset)
            {
                newData.modType = ModType.Map;
            }

            newData.modAsset = currentObject;
            var path = AssetDatabase.GetAssetPath(currentObject);
            path = Path.GetDirectoryName(path);
            path = Path.Combine(path, $"{name}ModData.asset");
            AssetDatabase.CreateAsset(newData, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newData;
        }


        public static async void CreateAsset(ModProfile profile)
        {
            var modName = profile.name;
            var assets = AssetDatabase.FindAssets(modName + "ModData");
            if (assets.Length > 0)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(assets[0]));
                return;
            }
            BuiltModData newData = ScriptableObject.CreateInstance<BuiltModData>();

            var modID = profile.id;
            var summary = profile.summary;
            var description = profile.description;
            var version = profile.latestVersion;
            var logo = await ModIOUnityAsync.DownloadTexture(profile.logoImageOriginal);
            var folder = "Assets/Generated Logos";
            var fileName = modName + "-Logo" + ".asset";
            var completePath = folder + '/' + fileName;
            
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            
            AssetDatabase.CreateAsset(logo.value, completePath);
            var previewAsset = AssetDatabase.LoadAssetAtPath(completePath, typeof(Texture2D));
            var actualLogo = (Texture2D)EditorUtility.InstanceIDToObject(previewAsset.GetInstanceID());

            var uploaded = profile.modfile.filesize > 0;
            var visible = profile.visible;
            var metaData = profile.metadata;
            var tags = profile.tags;
            ModType modType = ModType.None;
            foreach (var tag in tags)
            {
                if (tag == ModType.Spawnable.ToString())
                {
                    modType = ModType.Spawnable;
                    break;
                }

                if (tag == ModType.Map.ToString())
                {
                    modType = ModType.Map;
                    break;
                }
            }
            
            
            newData.modName = modName;
            newData.modSummary = summary;
            newData.modDescription = description;
            newData.modVersion = version;
            newData.modType = modType;
            newData.modMetaData = metaData;
            newData.modIdCache = modID;
            newData.isPublic = visible;
            newData.logo = actualLogo;
            newData.isUploaded = uploaded;
    
            AssetDatabase.CreateAsset(newData, $"Assets/{modName}ModData.asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newData;
        }
    }
}
#endif