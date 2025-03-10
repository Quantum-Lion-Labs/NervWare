#if UNITY_EDITOR
using System.IO;
using System.Threading.Tasks;
using ModIO;
using NervWareSDK.Packaging;
using SaintsField;
using SaintsField.Playa;
using UnityEditor;
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
        [AboveImage(nameof(GetHeaderImage), align: EAlign.Center, order = -10)]
        [LayoutStart("Mod Data", ELayout.TitleBox)]
        [InfoBox("Information about your mod goes here.")]
        [GUIColor(EColor.Pink)]
        [ValidateInput(nameof(ValidateName))]
        public string modName = "My Mod";

        [TextArea] public string modSummary = "My Excellent Mod Summary";
        [TextArea] public string modDescription = "My Excellent Mod Description";
        public string modVersion = "0.0.1";
        public ModType modType = ModType.Spawnable;
        [HideInInspector]
        [GUIColor(EColor.Red)] [TextArea] public string modMetaData = "metaDataTodo";

        [LayoutEnd("Mod Data")]
        [LayoutStart("Mod Assets", ELayout.TitleBox)]
        [InfoBox("Setup your mod assets here.")]
        [GUIColor(EColor.Cyan)]
        [AssetPreview(groupBy: "Previews")]
        [ValidateInput(nameof(ValidatePrefab))]
        public GameObject prefab;

        [AssetPreview(groupBy: "Previews")] [PostFieldButton(nameof(GenerateTempLogo))]
        public Texture2D logo;

        
        [LayoutEnd("Mod Data")] 
        [ReadOnly] [HideInInspector]
        public string androidBuildPath;

        [ReadOnly] [HideInInspector]
        public string windowsBuildPath;

        [ReadOnly] [HideInInspector]
        public long modIdCache = -1;

        [ReadOnly]
        [ShowIf(nameof(ShowProgress))]
        [ProgressBar(min: 0f, max: 1f, step: 0.01f, EColor.Blue, EColor.CharcoalGray, null, null,
            nameof(GetProgressTitle))]
        public float progress;

        [ReadOnly] [HideInInspector]
        public string progressTitle = "";

        [ContextMenu("Reset Mod ID")]
        private void ResetModID()
        {
            modIdCache = -1;
        }

        [Button]
        private void BuildMod()
        {
            ModPackager packager = new ModPackager(this);
            packager.PackMod();
        }

        [Button]
        private async void UploadMod()
        {
            ModUploader uploader = new ModUploader(this);
            await uploader.Upload();
        }

        private string GetProgressTitle(float curValue, float min, float max, string label)
        {
            return progressTitle;
        }

        private bool ShowProgress()
        {
            return progress > 0;
        }

        [Button]
        private void OpenModFolder()
        {
            Debug.Log(windowsBuildPath);
            if (Directory.Exists(windowsBuildPath))
            {
                string fullPath = Path.GetFullPath(windowsBuildPath + "/../");
                EditorUtility.RevealInFinder(fullPath);
            }
            else if (Directory.Exists(androidBuildPath ))
            {
                string fullPath = Path.GetFullPath(androidBuildPath + "/../");
                EditorUtility.RevealInFinder(fullPath);
            }
        }

        [Button]
        private async void OpenModPageInBrowser()
        {
            if (modIdCache < 0) return;
            var mod = await ModIOUnityAsync.GetMod(new ModId(modIdCache));
            if (mod.result.Succeeded())
            {
                var id = mod.value.nameId;
                Application.OpenURL("https://mod.io/g/nervbox/m/" + id);
            }
        }

        private string ValidateName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Mod name cannot be empty!";
            }

            return null;
        }

        private string ValidatePrefab(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return "Missing prefab!";
            }

            if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                return null;
            }

            return "Prefab is part of a scene or invalid!";
        }

        private async void GenerateTempLogo()
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

            Debug.Log($"Colors are: {backgroundColorString} and {textColorString}");

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
    }
}
#endif