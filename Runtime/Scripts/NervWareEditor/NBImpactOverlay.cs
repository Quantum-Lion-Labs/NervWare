#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NervBox.Interaction;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), displayName: "Surface Painter", id = "surface-painter", defaultDisplay = true)]
public class NBImpactOverlay : Overlay
{
    private VisualElement _root;
    private Dictionary<SurfaceType, VisualElement> _paletteButtons = new();
    
    public override VisualElement CreatePanelContent()
    {
        _root = new VisualElement() {name = "root-container"};
        var styleSheet =
            AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.quantumlionlabs.nervwaresdk/Editor/Resources/ImpactPaletteStyle.uss");
        _root.styleSheets.Add(styleSheet);
        Button missing = new Button(() =>
        {
            NBImpact.ShowMissing = !NBImpact.ShowMissing;
        })
        {
            text = "Toggle Missing Collider View"
        };
        _root.Add(missing);

        Button meta = new Button(NBImpactPainter.EnsureMetaAudio)
        {
            text = "Validate Meta Audio Components"
        };
        _root.Add(meta);
        var outerButtonRow = new VisualElement();
        outerButtonRow.AddToClassList("button-row");
        var swatch = new VisualElement();
        swatch.AddToClassList("color-swatch");
        swatch.style.backgroundColor = Color.magenta;
        var outLabel = new Label("Missing");
        outLabel.AddToClassList("button-label");
        outerButtonRow.Add(swatch);
        outerButtonRow.Add(outLabel);
        _root.Add(outerButtonRow);
        _paletteButtons.Clear();
        foreach (var pair in NBImpactPainter.SurfaceColors)
        {
            SurfaceType surface = pair.Key;
            var buttonRow = new VisualElement();
            buttonRow.AddToClassList("button-row");
            var colorSwatch = new VisualElement();
            colorSwatch.AddToClassList("color-swatch");
            colorSwatch.style.backgroundColor = pair.Value;
            
            
            var label = new Label(surface.ToString());
            label.AddToClassList("button-label");
            
            buttonRow.Add(colorSwatch);
            buttonRow.Add(label);
            
            buttonRow.RegisterCallback<MouseDownEvent>(evt =>
            {
                // Set the active material in the tool.
                NBImpactPainter.CurrentSurfaceType = surface;
                // Update the visual selection in the UI.
                UpdateSelection(surface);
            });
            
            _root.Add(buttonRow);
            _paletteButtons.Add(surface, buttonRow);
        }

        UpdateSelection(NBImpactPainter.CurrentSurfaceType);
        return _root;
    }

    private void UpdateSelection(SurfaceType type)
    {
        foreach (var pair in _paletteButtons)
        {
            if (pair.Key != type)
            {
                pair.Value.RemoveFromClassList("button-row-selected");
            }
            else
            {
                pair.Value.AddToClassList("button-row-selected");
            }
        }
    }
}

#endif