using NervBox.Tools.HandPoser;
using UnityEngine;

namespace NervBox.Interaction
{
    public enum HoldType
    {
        SwapGrip,
        OnlyOneHand,
        MultipleHands,
        LeftOnly,
        RightOnly
    }
    public abstract partial class NBGripBase : MonoBehaviour
    {
        [SerializeField] private bool canBeForceGrabbed;
        [SerializeField] public Vector3 grabRotationLimits = new(10f, 10f, 10f);
        [SerializeField] private HandPose pose;
        [SerializeField] private bool useProceduralPosingFallback = true;
        [SerializeField] private bool showIndicator = false;
        [SerializeField] private bool climbOverride = false;
        [SerializeField] private Vector3 heldPositionOffset = Vector3.zero;
        [SerializeField] private Vector3 heldEulerOffset = Vector3.zero;
        [SerializeField] protected float jointBreakForce = 15000f;
        [SerializeField] protected bool flipHorizontal = true;
        [SerializeField] protected Transform targetTransform;
        [SerializeField] protected float radius = 0f;
        [SerializeField] protected HoldType holdType = HoldType.MultipleHands;
        [SerializeField] internal bool requireAdditionalWristMotion = false;
        [field: SerializeField] public bool IgnoreCollideWithBody { get; private set; } = false;

        [field: SerializeField] public bool UseITCompensation { get; private set; } = true;
    }
}