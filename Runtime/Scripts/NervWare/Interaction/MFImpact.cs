using System;
using System.Collections;
using System.Collections.Generic;
using SaintsField;
using SaintsField.Playa;
using Realmsmith.Misc;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

namespace Realmsmith.Interaction
{
    /// <summary>
    /// A collision effect handler as well as a surface handler.
    /// </summary>
    public class MFImpact : MonoBehaviour
    {
        [LayoutGroup("Sound Settings", ELayout.TitleBox)]
        [Tooltip("If true, the object will play collision sounds.")]
        [SerializeField]
        private bool playImpactSound = true;

        [LayoutStart("Physics Properties", ELayout.TitleBox)]
        [Tooltip("If true, the object can always be stabbed.")]
        [SerializeField]
        private bool stabOverride = false;

        [SerializeField]
        [Tooltip("If true, define a custom center of mass. Recommended for advanced users.")]
        private bool overrideCOM = false;

        [ShowIf("overrideCOM")]
        [SerializeField]
        [Tooltip("Custom center of mass for the object.")]
        private Vector3 centerOfMass;

        [SerializeField]
        [Tooltip("Custom inertia tensor for the object. Recommended for advanced users.")]
        private bool overrideInertiaTensor;

        [ValidateInput(nameof(ValidateInertiaTensor))]
        [ShowIf("overrideInertiaTensor")]
        [SerializeField]
        [Tooltip("Custom inertia tensor for the object.")]
        private Vector3 inertiaTensor = Vector3.zero * 0.01f;

        [ShowIf("overrideInertiaTensor")] [SerializeField] 
        [Tooltip("Custom inertia tensor rotation for the object.")]
        private Vector3 inertiaTensorRotation = Vector3.zero;

        [LayoutStart("Surface Properties", ELayout.TitleBox)]
        [InfoBox(
            "Enable overrideSurfaceType to use NervBox's materials. If disabled, you must provide your impact material below.")]
        [SerializeField]
        private bool overrideSurfaceType = false;

        [ShowIf("overrideSurfaceType")]
        [SerializeField]
        [Tooltip("The desired override surface type for the object.")]
        private SurfaceType surfaceTypeOverride = SurfaceType.Default;

        [SerializeField]
        [Tooltip("If enabled, define a desired surface hardness for the object.")]
        private bool overrideSurfaceHardness = true;

        [ShowIf("overrideSurfaceHardness")]
        [SerializeField]
        [Tooltip("The desired override surface hardness for the object.")]
        private SurfaceHardness surfaceHardnessOverride = SurfaceHardness.None;

        [SerializeField]
        [Tooltip("Enable to allow multiple MFImpacts to be used on this object for multiple material properties.")]
        private bool isMultiMaterialObject = false;

        [LayoutGroup("Surface Properties", ELayout.TitleBox)]
        [HideIf("overrideSurfaceType")]
        [Expandable]
        [SerializeField]
        [Tooltip("Custom impact properties for the object.")]
        private MFImpactProperties properties = null;

        
        private string ValidateInertiaTensor()
        {
            if (inertiaTensor == Vector3.zero)
            {
                return "Inertia Tensor cannot be zero!";
            }

            return null;
        }
    }
}