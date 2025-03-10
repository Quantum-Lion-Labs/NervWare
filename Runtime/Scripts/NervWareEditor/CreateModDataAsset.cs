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
            return Selection.activeObject != null && PrefabUtility.IsPartOfAnyPrefab(Selection.activeObject);
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
            newData.prefab = (GameObject)currentObject;
            newData.modType = ModType.Spawnable;
            AssetDatabase.CreateAsset(newData, $"Assets/{name}ModData.asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newData;
        }
    }
}
#endif