using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NervWareSDK.Editor
{
    public class AssetPreviewWindow : EditorWindow
    {
        private List<Object> _assets;
        private Action<Object> _selectedCallback;
        private Vector2 _scrollPosition;
        private const float ItemSize = 90f;
        private const float ItemPadding = 10f;

        public static void Show(List<Object> assets, Action<Object> selectedCallback, string name)
        {
            AssetPreviewWindow window = GetWindow<AssetPreviewWindow>();
            window._assets = assets;
            window._selectedCallback = selectedCallback;
            window.minSize = new Vector2(300, 200);
            window.titleContent = new GUIContent(name);
        }

        private void OnGUI()
        {
            if (_assets == null || _assets.Count == 0)
            {
                EditorGUILayout.LabelField("No assets available");
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            int columns = Mathf.FloorToInt((position.width - ItemPadding) / (ItemSize + ItemPadding));

            int currentColumn = 0;

            EditorGUILayout.BeginVertical();

            for (int i = 0; i < _assets.Count; i++)
            {
                if (currentColumn == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                }

                var asset = _assets[i];
                if (asset == null) continue;

                EditorGUILayout.BeginVertical(GUILayout.Width(ItemPadding + ItemSize));
                EditorGUILayout.Space(ItemPadding / 2);
                Texture2D preview = AssetPreview.GetAssetPreview(asset);
                if (preview == null)
                {
                    preview = AssetPreview.GetMiniThumbnail(asset);
                }

                if (GUILayout.Button(preview, GUILayout.Width(ItemSize), GUILayout.Height(ItemSize)))
                {
                    _selectedCallback?.Invoke(asset);
                    Close();
                }

                GUILayout.Label(asset.name, EditorStyles.miniLabel, GUILayout.Width(ItemSize), 
                    GUILayout.Height(20));
               GUILayout.Space(ItemPadding / 2);
               EditorGUILayout.EndVertical();
               
               currentColumn++;

               if (currentColumn >= columns || i == _assets.Count - 1)
               {
                   EditorGUILayout.EndHorizontal();
                   currentColumn = 0;
                   if (i < _assets.Count - 1)
                   {
                       GUILayout.Space(ItemPadding);
                   }
               }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
    }
}