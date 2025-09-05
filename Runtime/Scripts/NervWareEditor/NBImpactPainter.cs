
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NervBox.Interaction;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using Object = UnityEngine.Object;

[EditorTool("Impact Painter", null)]
public class NBImpactPainter : EditorTool
{
    private GUIContent _toolbarIcon;

    public override GUIContent toolbarIcon => _toolbarIcon;
    private static SurfaceType _currentSurfaceType = SurfaceType.Default;
    private static List<GameObject> allGameObjects = new();
    private static HashSet<GameObject> collidersMissingImpactProperties = new();

    private const float SurfaceAlpha = 0.85f;

    public static Dictionary<SurfaceType, Color> SurfaceColors = new Dictionary<SurfaceType, Color>()
    {
        { SurfaceType.NoSurface, new Color(0.5f, 0.5f, 0.5f, SurfaceAlpha) },

        { SurfaceType.Brick, new Color(0.7f, 0.25f, 0.2f, SurfaceAlpha) },
        { SurfaceType.Concrete, new Color(0.45f, 0.45f, 0.47f, SurfaceAlpha) },
        { SurfaceType.Glass, new Color(0.6f, 0.85f, 0.9f, 0.35f) },
        { SurfaceType.Plaster, new Color(0.85f, 0.8f, 0.7f, SurfaceAlpha) },
        { SurfaceType.Wood, new Color(0.6f, 0.4f, 0.2f, SurfaceAlpha) },
        { SurfaceType.Thatch, new Color(0.8f, 0.7f, 0.3f, SurfaceAlpha) },

        { SurfaceType.Dirt, new Color(0.4f, 0.25f, 0.15f, SurfaceAlpha) },
        { SurfaceType.Grass, new Color(0.3f, 0.7f, 0.2f, SurfaceAlpha) },
        { SurfaceType.Gravel, new Color(0.5f, 0.45f, 0.4f, SurfaceAlpha) },
        { SurfaceType.Rock, new Color(0.3f, 0.3f, 0.35f, SurfaceAlpha) },

        { SurfaceType.Carpet, new Color(0.2f, 0.5f, 0.6f, SurfaceAlpha) },
        { SurfaceType.Ceramic, new Color(0.9f, 0.9f, 0.95f, SurfaceAlpha) },
        { SurfaceType.Metal, new Color(0.7f, 0.75f, 0.8f, SurfaceAlpha) },

        { SurfaceType.Default, new Color(0.7f, 0.7f, 0.65f, SurfaceAlpha) }
    };

    private static readonly int Color1 = Shader.PropertyToID("_Color");

    private Material _highlightMaterial;
    private Material _missingMaterial;
    private List<Renderer> _impactMaterialTargets = new();
    private List<Renderer> _otherMaterialTargets = new();
    public static SurfaceType CurrentSurfaceType
    {
        get => _currentSurfaceType;
        set => _currentSurfaceType = value;
    }

    private void OnEnable()
    {
        _toolbarIcon = new GUIContent()
        {
            image = EditorGUIUtility.IconContent("ClothInspector.PaintTool").image,
            text = "Impact Painter",
            tooltip = "Paints Impact Materials onto static colliders"
        };

        allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None).ToList();
        collidersMissingImpactProperties.Clear();
        foreach (var gameObj in allGameObjects)
        {
            if (gameObj.TryGetComponent<Collider>(out var col) && !col.isTrigger && gameObj.isStatic)
            {
                if (!gameObj.GetComponent<NBImpact>() && !collidersMissingImpactProperties.Contains(gameObj))
                {
                    collidersMissingImpactProperties.Add(gameObj);
                }
            }
        }

        _highlightMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.quantumlionlabs.nervwaresdk/Editor/Resources/Materials/EditorHighlight.mat");
        _missingMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.quantumlionlabs.nervwaresdk/Editor/Resources/Materials/EditorMissing.mat");
        RefreshDrawingTargets();
        EditorApplication.hierarchyChanged += RefreshDrawingTargets;
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyChanged -= RefreshDrawingTargets;
    }

    public static void EnsureMetaAudio()
    {
        foreach (var impact in FindObjectsByType<NBImpact>(FindObjectsSortMode.None))
        {
            if (!impact.gameObject.isStatic)
            {
                continue;
            }

            ApplyMetaAudio(impact);
        }
    }

    private void RefreshDrawingTargets()
    {
        _impactMaterialTargets.Clear();
        _otherMaterialTargets.Clear();
        var colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        foreach (var collider in colliders)
        {
            var renderer = collider.GetComponentInParent<MeshRenderer>();
            if (!renderer)
            {
                continue;
            }
            if (collider.TryGetComponent(out NBImpact _))
            {
                _impactMaterialTargets.Add(renderer);
            }
            else
            {
                _otherMaterialTargets.Add(renderer);
            }
        }

        SceneView.RepaintAll();
    }

    private static void ApplyMetaAudio(NBImpact impact)
    {
        if (impact.gameObject.GetComponentInParent<Rigidbody>() != null)
        {
            return;
        }
        const string geoFullyQualifiedName =
            "MetaXRAcousticGeometry, Meta.XR.Acoustics, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        var geo = impact.gameObject.GetComponent(Type.GetType(geoFullyQualifiedName));
        var material = impact.gameObject.GetComponent<MetaXRAcousticMaterial>();
        if (geo == null || material == null)
        {
            Undo.RecordObject(impact.gameObject, "Add Surface Material");
            if (material == null)
            {
                material = Undo.AddComponent<MetaXRAcousticMaterial>(impact.gameObject);
                var field = material.GetType()
                    .GetField("properties", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(material, ImpactMaterialToMetaXRMaterial(impact.SurfaceTypeOverride));
                }
            }

            if (geo == null)
            {
                Undo.AddComponent(impact.gameObject, Type.GetType(geoFullyQualifiedName));
            }

            EditorUtility.SetDirty(impact.gameObject);
        }
    }

    private static Object ImpactMaterialToMetaXRMaterial(SurfaceType surface)
    {
        const string metaPath =
            "Packages/com.quantumlionlabs.nervwaresdk/Runtime/ScriptableObjects/MetaXR Materials";
        if (surface == SurfaceType.NoSurface)
        {
            return null;
        }

        var result =
            AssetDatabase.LoadAssetAtPath<Object>(metaPath + Path.DirectorySeparatorChar + surface + ".asset");
        if (!result)
        {
            Debug.LogError($"No asset found for surface type {surface}!");
        }

        return result;
    }

    public override void OnToolGUI(EditorWindow window)
    {
        Event e = Event.current;

        if (window is not SceneView)
        {
            return;
        }

      

        if (e.type == EventType.Repaint)
        {
            if (_highlightMaterial)
            {
                foreach (var materialTarget in _impactMaterialTargets)
                {
                    if (materialTarget == null) continue;
                    NBImpact impact = materialTarget.GetComponentInChildren<NBImpact>();
                    MeshFilter meshFilter = materialTarget.GetComponentInChildren<MeshFilter>();
                    if (impact && meshFilter && meshFilter.sharedMesh)
                    {
                        _highlightMaterial.SetColor(Color1, SurfaceColors[impact.SurfaceTypeOverride]);
                        _highlightMaterial.SetPass(0);
                        Graphics.DrawMeshNow(meshFilter.sharedMesh, materialTarget.transform.localToWorldMatrix);
                    }
                }

                if (NBImpact.ShowMissing)
                {
                    foreach (var materialTarget in _otherMaterialTargets)
                    {
                        MeshFilter meshFilter = materialTarget.GetComponentInChildren<MeshFilter>();
                        if (!meshFilter)
                        {
                            continue;
                        }

                        if (!meshFilter.sharedMesh)
                        {
                            continue;
                        }
                        if (!collidersMissingImpactProperties.Contains(materialTarget.gameObject)) continue;
                        _missingMaterial.SetPass(0);
                        Graphics.DrawMeshNow(meshFilter.sharedMesh, materialTarget.transform.localToWorldMatrix);
                    }
                }
            }
        }


        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlId);
        if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
        {
            if (e.button == 0)
            {
                Paint(e.mousePosition);
                e.Use();
            }
        }
    }

    private static void DrawText(string text, Vector3 pos)
    {
        Handles.BeginGUI();
        GUI.color = Gizmos.color;
        pos = Gizmos.matrix.MultiplyPoint(pos);
        var view = SceneView.currentDrawingSceneView;
        if (view == null)
            return;
        Vector3 screenPos = view.camera.WorldToScreenPoint(pos);
        if (screenPos.y < 0 || screenPos.y > Screen.height || screenPos.x < 0 || screenPos.x > Screen.width ||
            screenPos.z < 0)
        {
            Handles.EndGUI();
            return;
        }

        Vector2 size = GUI.skin.label.CalcSize(new GUIContent(text));
        GUI.Label(
            new Rect(screenPos.x - (size.x / 2), view.position.height - screenPos.y - (size.y * 2f), size.x,
                size.y), text);
        Handles.EndGUI();
    }


    private void Paint(Vector2 eMousePosition)
    {
        GameObject pickedObject = HandleUtility.PickGameObject(eMousePosition, false);

        if (pickedObject == null)
            return;

        if (pickedObject.TryGetComponent<Collider>(out var collider))
        {
            NBImpact impact = pickedObject.GetComponent<NBImpact>();
            if (!impact)
            {
                impact = Undo.AddComponent<NBImpact>(pickedObject);
                if (collidersMissingImpactProperties.Contains(pickedObject))
                {
                    collidersMissingImpactProperties.Remove(pickedObject);
                }
            }

            if (impact.SurfaceTypeOverride != _currentSurfaceType)
            {
                Undo.RecordObject(impact, "Change Material");
                impact.SurfaceTypeOverride = _currentSurfaceType;
                EditorUtility.SetDirty(impact);
            }

            ApplyMetaAudio(impact);
        }
    }
}

