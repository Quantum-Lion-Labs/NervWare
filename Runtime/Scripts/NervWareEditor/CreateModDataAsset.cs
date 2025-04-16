#if UNITY_EDITOR
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
            if (currentObject is GameObject gameObject)
            {
                newData.prefab = gameObject;
                newData.modType = ModType.Spawnable;
            }
            else if (currentObject is SceneAsset asset)
            {
                newData.scene = asset;
                newData.modType = ModType.Map;
            }

            AssetDatabase.CreateAsset(newData, $"Assets/{name}ModData.asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newData;
        }
    }
}
#endif