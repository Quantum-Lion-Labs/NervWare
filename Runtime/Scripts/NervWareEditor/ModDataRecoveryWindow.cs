#if UNITY_EDITOR
using System;
using ModIO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NervWareSDK.Editor
{
    public class ModDataRecoveryWindow : EditorWindow
    {
        [MenuItem("NervWare/Recover Mod Data from ModID")]
        public static void ShowWindow()
        {
            ModDataRecoveryWindow wnd = GetWindow<ModDataRecoveryWindow>();
            wnd.titleContent = new GUIContent("Mod Data Recovery");
            wnd.minSize = new Vector2(528, 300);
        }

        private IntegerField _idField;

        private void CreateGUI()
        {
            //pad root
            var root = rootVisualElement;
            root.style.paddingLeft = 15;
            root.style.paddingRight = 15;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;

            //warning label
            var label = new Label(
                "If you've deleted your mod data for a mod you worked on, " +
                "you can recover it by entering its ModID found on its mod page." +
                " If you are not the creator of the mod this will not work! " +
                "This will allow you to re-upload your mods if you have lost the original mod data assets." );
            label.style.height = 100;
            label.style.fontSize = 16;
            label.style.whiteSpace = WhiteSpace.Normal;

            root.Add(label);
            
            //mod field
            _idField = new IntegerField("Mod ID");
            _idField.style.marginTop = 5;
            _idField.style.marginBottom = 10;

            var idFieldLabel = _idField.Q<Label>(className: Label.ussClassName);
            if (idFieldLabel != null)
            {
                idFieldLabel.style.minWidth = 80;
                idFieldLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            }

            root.Add(_idField);

            //recovery button
            var button = new Button(TryRecoverModData) { text = "Try Recover Mod Data" };
            button.style.height = 30;
            button.style.marginTop = 10;
            button.style.backgroundColor = new Color(0.15f, 0.35f, 0.6f);
            button.style.color = Color.white;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.borderTopLeftRadius = 5;
            button.style.borderTopRightRadius = 5;
            button.style.borderBottomLeftRadius = 5;
            button.style.borderBottomRightRadius = 5;
            button.style.borderTopWidth = 0;
            button.style.borderRightWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.borderLeftWidth = 0;
            button.style.borderTopColor = Color.white;
            button.style.borderRightColor = Color.white;
            button.style.borderLeftColor = Color.white;
            button.style.borderBottomColor = Color.white;
            
            //hover styling
            button.RegisterCallback<PointerEnterEvent>(evt =>
            {
                button.style.borderTopWidth = 2;
                button.style.borderRightWidth = 2;
                button.style.borderBottomWidth = 2;
                button.style.borderLeftWidth = 2;
                
            });
            button.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                button.style.borderTopWidth = 0;
                button.style.borderRightWidth = 0;
                button.style.borderBottomWidth = 0;
                button.style.borderLeftWidth = 0;
            });
            root.Add(button);
        }

        private async void TryRecoverModData()
        {
            if (_idField.value <= 0)
            {
                EditorUtility.DisplayDialog("Recovery Error", "Mod ID is invalid!", "ok");
                return;
            }

            var user = await ModIOUnityAsync.GetCurrentUser();
            if (!user.result.Succeeded())
            {
                EditorUtility.DisplayDialog("Recovery Error",
                    $"ModIO Failed with message: {user.result.message}. See console for more details.", "ok");
                Debug.LogError($"Recovery Error: {user.result.message} {user.result.apiMessage} {user.result.apiCode}");
                return;
            }

            var mod = await ModIOUnityAsync.GetMod(new ModId(_idField.value));
            if (!mod.result.Succeeded())
            {
                EditorUtility.DisplayDialog("Recovery Error",
                    $"ModIO Failed to get mod with message {mod.result.message}." +
                    $" Maybe your ModID is incorrect?", "ok");
                Debug.LogError($"Recovery Error: {mod.result.message} {mod.result.apiMessage} {mod.result.apiCode}");
                return;
            }

            if (mod.value.creator.userId != user.value.userId)
            {
                EditorUtility.DisplayDialog("Recovery Error", 
                    $"You are not the creator of this mod!", "ok");
                return;
            }
            
            //finally create the mod data
            CreateModDataAsset.CreateAsset(mod.value);
        }
    }
}
#endif