/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/************************************************************************************
 * Filename    :   MetaXRAcousticSettings.cs
 * Content     :   Exposes acoustic settings MetaXRAudioUnity
 ***********************************************************************************/

using Meta.XR.Acoustics;
using UnityEngine;
using Native = MetaXRAcousticNativeInterface;

/// \brief This class exposes controls for the Meta XR Acoustics SDK at a project level
public class MetaXRAcousticSettings : ScriptableObject
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnBeforeSceneLoadRuntimeMethod()
    {
        Instance.ApplyAllSettings();
    }

    [Tooltip("This is the path inside your Unity project which will store all baked geometry files.")]
    internal const string AcousticFileRootDir = "Scenes";

    [SerializeField]
    [Tooltip("Select which type of acoustic modeling system is used to generate reverb and reflections.")]
    private AcousticModel acousticModel = Meta.XR.Acoustics.AcousticModel.Automatic;

    /// \brief The reflections engine used during runtime
    /// \see AcousticModel
    public AcousticModel AcousticModel
    {
        get => acousticModel;
        set
        {
            if (value != acousticModel)
            {
                acousticModel = value;
                Native.Interface.SetAcousticModel(value);
            }
        }
    }

    [SerializeField]
    [Tooltip("When enabled and using geometry, all spatailized AudioSources will diffract (propagate around corners and obstructions)")]
    private bool diffractionEnabled = true;

    /// \brief When enabled and using geometry, all spatailized AudioSources will diffract (propagate around corners and obstructions)
    internal bool DiffractionEnabled
    {
        get => diffractionEnabled;
        set
        {
            if (value != diffractionEnabled)
            {
                diffractionEnabled = value;
                Native.Interface.SetEnabled(EnableFlagInternal.DIFFRACTION, value);
            }
        }
    }
#if OVR_INTERNAL_CODE
    [SerializeField]
    [Tooltip("Enable fallback to dynamic real-time simulation when no Acoustic Map is enabled")]
    private bool dynamicSimulationEnabled = false;

    /// \brief Enable fallback to dynamic real-time simulation when no Acoustic Map is enabled
    internal bool DynamicSimulationEnabled
    {
        get => dynamicSimulationEnabled;
        set
        {
            if (value != dynamicSimulationEnabled)
            {
                dynamicSimulationEnabled = value;
                Native.Interface.SetEnabled(EnableFlagInternal.DYNAMIC_SIMULATION, value);
            }
        }
    }

    [SerializeField]
    [Tooltip("Higher values produce better quality but use more resources, 100 is default. Has no effect if using Acoustic Map")]
    [Range(1.0f, 200.0f)]
    private float dynamicQuality = 100.0f;

    /// \brief quality as a percentage. Higher values produce better quality but use more resources, 100 is default. Has no effect if using Acoustic Map
    internal float DynamicQuality
    {
        get => dynamicQuality;
        set
        {
            if (value != dynamicQuality)
            {
                dynamicQuality = value;
                Native.Interface.SetPropagationQuality(value / 100.0f);
            }
        }
    }

    [SerializeField]
    [Tooltip("The number of diffraction events that can occur along a single sound propagation path. Has no effect if using Acoustic Map.")]
    [Range(1, 10)]
    private int dynamicDiffractionOrder = 10;

    /// \brief The number of diffraction events that can occur along a single sound propagation path. Has no effect if using Acoustic Map.
    internal int DynamicDiffractionOrder
    {
        get => dynamicDiffractionOrder;
        set
        {
            if (value != dynamicDiffractionOrder)
            {
                dynamicDiffractionOrder = value;
                Native.Interface.SetDiffractionOrder(value);
            }
        }
    }
#endif
    [SerializeField]
    [Tooltip("Geometry will exclude children with these tags")]
    private string[] excludeTags = new string[0];

    /// \brief Geometry will exclude children with these tags
    internal string[] ExcludeTags { get => excludeTags; set => excludeTags = value; }

    [SerializeField]
    [Tooltip("When you bake an acoustic map, also bake all the acoustic geometry files")]
    private bool mapBakeWriteGeo = true;

    /// \brief When you bake an acoustic map, also bake all the acoustic geometry files
    [Tooltip("If enabled, acoustic geometry files will also be written when baking an acoustic map")]
    internal bool MapBakeWriteGeo
    {
        get => mapBakeWriteGeo;
        set => mapBakeWriteGeo = value;
    }

    /// Provide all current settings to the native DLL.
    internal void ApplyAllSettings()
    {
        Debug.Log("Applying Acoustic Propagation Settings: " +
            $"[acoustic model = {AcousticModel}], " +
#if OVR_INTERNAL_CODE
            $"[diffraction = {DiffractionEnabled}], " +
            $"[dynamic simulation = {DynamicSimulationEnabled}]" +
            $"[quality = {DynamicQuality}%]" +
            $"[diffraction order = {DynamicDiffractionOrder}]");
#else
            $"[diffraction = {DiffractionEnabled}], ");
#endif
        Native.Interface.SetAcousticModel(AcousticModel);
        Native.Interface.SetEnabled(EnableFlagInternal.DIFFRACTION, DiffractionEnabled);
#if OVR_INTERNAL_CODE
        Native.Interface.SetEnabled(EnableFlagInternal.DYNAMIC_SIMULATION, DynamicSimulationEnabled);
        Native.Interface.SetPropagationQuality(DynamicQuality / 100.0f);
        Native.Interface.SetDiffractionOrder(DynamicDiffractionOrder);
#endif
    }

    private static MetaXRAcousticSettings instance;

    /// \brief The singleton instance of the project settings
    public static MetaXRAcousticSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<MetaXRAcousticSettings>("MetaXRAcousticSettings");

                // This can happen if the developer never input their App Id into the Unity Editor
                // and therefore never created the OculusPlatformSettings.asset file
                // Use a dummy object with defaults for the getters so we don't have a null pointer exception
                if (instance == null)
                {
                    instance = ScriptableObject.CreateInstance<MetaXRAcousticSettings>();

#if UNITY_EDITOR
                    // Only in the editor should we save it to disk
                    string properPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "Resources");
                    if (!System.IO.Directory.Exists(properPath))
                    {
                        UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                    }

                    string fullPath = System.IO.Path.Combine(
                        "Assets", "Resources", "MetaXRAcousticSettings.asset");
                    UnityEditor.AssetDatabase.CreateAsset(instance, fullPath);
#endif
                }
            }

            return instance;
        }
    }
}
