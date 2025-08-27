using System;
using System.Collections;
using System.Collections.Generic;
using NervBox.Misc;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

namespace NervBox.Interaction
{
    /// <summary>
    /// A collision effect handler as well as a surface handler.
    /// </summary>
    public class NBImpact : MonoBehaviour
    {
        [Tooltip("If true, the object will play collision sounds.")]
        [SerializeField]
        private bool playImpactSound = true;

        [Tooltip("If true, the object can always be stabbed.")]
        [SerializeField]
        private bool stabOverride = false;

        [SerializeField]
        [Tooltip("If true, define a custom center of mass. Recommended for advanced users.")]
        private bool overrideCOM = false;

        [SerializeField]
        [Tooltip("Custom center of mass for the object.")]
        private Vector3 centerOfMass;

        [SerializeField]
        [Tooltip("Custom inertia tensor for the object. Recommended for advanced users.")]
        private bool overrideInertiaTensor;

        [SerializeField]
        [Tooltip("Custom inertia tensor for the object.")]
        private Vector3 inertiaTensor = Vector3.zero * 0.01f;

        [Tooltip("Custom inertia tensor rotation for the object.")]
        private Vector3 inertiaTensorRotation = Vector3.zero;

        [SerializeField]
        private bool overrideSurfaceType = false;

        [SerializeField]
        [Tooltip("The desired override surface type for the object.")]
        private SurfaceType surfaceTypeOverride = SurfaceType.Default;

        [SerializeField]
        [Tooltip("If enabled, define a desired surface hardness for the object.")]
        private bool overrideSurfaceHardness = true;

        [SerializeField]
        [Tooltip("The desired override surface hardness for the object.")]
        private SurfaceHardness surfaceHardnessOverride = SurfaceHardness.None;

        [SerializeField]
        [Tooltip("Enable to allow multiple MFImpacts to be used on this object for multiple material properties.")]
        private bool isMultiMaterialObject = false;

        // [LayoutGroup("Surface Properties", ELayout.TitleBox)]
        // [HideIf("overrideSurfaceType")]
        // [Expandable]
        [SerializeField]
        // [Tooltip("Custom impact properties for the object.")]
        private NBImpactProperties properties = null;

        [SerializeField] private bool useCustomProperties = false;
        [SerializeField] private AudioClip[] impactClips;
        public SurfaceType SurfaceTypeOverride
        {
            get => surfaceTypeOverride;
            set => surfaceTypeOverride = value;
        }

        public static bool ShowMissing { get; set; }

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