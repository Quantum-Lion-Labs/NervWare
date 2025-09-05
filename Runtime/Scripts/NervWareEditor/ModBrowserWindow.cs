using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NervWareSDK.Editor
{
    public class ModBrowserWindow : EditorWindow
    {
        private VisualTreeAsset _window;
        private StyleSheet _styleSheet;
        private VisualTreeAsset _listItem;

        private ListView _listView;
        private TextField _searchBar;

        private List<BuiltModData> _modDatas = new();

        [MenuItem("NervWare/Mod Browser")]
        public static void ShowWindow()
        {
            ModBrowserWindow wnd = GetWindow<ModBrowserWindow>();
            wnd.titleContent = new GUIContent("Mod Browser");
            wnd.Show();
        }

        public void CreateGUI()
        {
            //load in our uxml & style sheets
            _window = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.quantumlionlabs.nervwaresdk/Editor/Resources/ModBrowser.uxml");
            _styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Packages/com.quantumlionlabs.nervwaresdk/Editor/Resources/ModBrowser.uss");
            rootVisualElement.styleSheets.Add(_styleSheet);
            //copy it over
            _window.CloneTree(rootVisualElement);

            //grab items we'll be messing with often
            _listView = rootVisualElement.Q<ListView>("results-list");
            _listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            _searchBar = rootVisualElement.Q<TextField>("search-bar");

            _modDatas = FindModDatas();

            SetupListView();

            _searchBar.RegisterValueChangedCallback(OnSearchTextChanged);

            PopulateListView(_modDatas);
        }

        private List<BuiltModData> FindModDatas()
        {
            //sort by mod name
            return AssetDatabase.FindAssets($"t:{nameof(BuiltModData)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<BuiltModData>(AssetDatabase.GUIDToAssetPath(guid)))
                .OrderBy(data => data.modName).ToList();
        }
        
        private void SetupListView()
        {
            //called when an item is created
            _listView.makeItem = () =>
            {
                var container = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center
                    }
                };

                var icon = new Image
                {
                    scaleMode = ScaleMode.ScaleToFit
                };

                var label = new Label
                {
                    style =
                    {
                        flexGrow = 1
                    }
                };

                var visibility = new Label
                {
                    name = "visibility",
                    enableRichText = true
                };

                container.AddToClassList("list-item-container");
                icon.AddToClassList("list-item-icon");
                label.AddToClassList("list-item-label");
                visibility.AddToClassList("list-item-label");
                
                container.Add(icon);
                container.Add(label);
                container.Add(visibility);
                return container;
            };

            //called when data is bound
            _listView.bindItem = (element, i) =>
            {
                var item = _listView.itemsSource[i] as BuiltModData;
                var icon = element.Q<Image>();
                var label = element.Q<Label>();
                var visibility = element.Q<Label>("visibility");

                if (!item)
                {
                    label.text = "Unknown Mod";
                    icon.image = GetMissingIcon();
                }
                else
                {
                    label.text = item.modName;
                    bool modCreated = item.modIdCache != -1;
                    string text = modCreated ? (item.isPublic ? "Public" : "Private") : "No Mod Page";
                    string visibilityColor = modCreated ? (item.isPublic ? "green" : "yellow") : "red";
                    visibility.text = $"<color={visibilityColor}>{text}</color>";
                    icon.image = item.logo ? item.logo : GetMissingIcon();
                }
            };
            
            _listView.selectionChanged += ListViewOnselectionChanged;
            _listView.itemsChosen += ListViewOnitemsChosen;
        }

        private void ListViewOnitemsChosen(IEnumerable<object> selected)
        {
            //called when a double click occurs
            var selectedItem = selected.FirstOrDefault();
            if (selectedItem is not BuiltModData data) return;
            if (data.modAsset != null)
            {
                AssetDatabase.OpenAsset(data.modAsset);
            }
        }

        private void ListViewOnselectionChanged(IEnumerable<object> selected)
        {
            //called when a single click occurs
            var selectedItem = selected.FirstOrDefault() as BuiltModData;
            if (selectedItem == null) return;
            Selection.activeObject = selectedItem;
            EditorGUIUtility.PingObject(selectedItem);
        }

        private static Texture2D GetMissingIcon()
        {
            return EditorGUIUtility.IconContent("ScriptableObject Icon").image as Texture2D;
        }

        private void PopulateListView(List<BuiltModData> modDatas)
        {
            _listView.itemsSource = modDatas;
            _listView.Rebuild();
        }

        private void OnSearchTextChanged(ChangeEvent<string> evt)
        {
            string text = evt.newValue.ToLower();
            if (string.IsNullOrEmpty(text))
            {
                PopulateListView(_modDatas);
            }
            else
            {
                //just checking names...for now
                var filtered = _modDatas.Where(data => data.modName.ToLower().Contains(text)).ToList();
                PopulateListView(filtered);
            }
        }


    }
}