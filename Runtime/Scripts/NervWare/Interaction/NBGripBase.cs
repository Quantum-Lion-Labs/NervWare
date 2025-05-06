using NervBox.Tools.HandPoser;
using SaintsField;
using SaintsField.Playa;
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
        [Tooltip("Whether or not you can force grab this grip.")]
        [SerializeField] private bool canBeForceGrabbed;
        [Tooltip("The amount of play in each direction this object has when held in hand.")]
        [SerializeField] public Vector3 grabRotationLimits = new(10f, 10f, 10f);
        [SerializeField] private HandPose pose;
        [Tooltip("Whether to use the procedural hand pose or the fist pose when no pose is assigned.")]
        [SerializeField] private bool useProceduralPosingFallback = true;
        [Tooltip("Whether to show the grab indicator")]
        [SerializeField] private bool showIndicator = false;
        [SerializeField] private bool climbOverride = false;
        [Tooltip("Controller offset for position")]
        [SerializeField] private Vector3 heldPositionOffset = Vector3.zero;
        [Tooltip("Controller offset for rotation")]
        [SerializeField] private Vector3 heldEulerOffset = Vector3.zero;
        [Tooltip("How many newtons it takes for this joint to break")]
        [SerializeField] protected float jointBreakForce = 15000f;
        [Tooltip("Whether this grip should flip between left and right hands or stay the same.")]
        [SerializeField] protected bool flipHorizontal = true;
        [Tooltip("The transform to move the palm to. If null the transform of this grip will be used.")]
        [SerializeField] protected Transform targetTransform;
        [Tooltip("How far the palm will sit from the target transform")]
        [SerializeField] protected float radius = 0f;
        [Tooltip("How this object is handled when held with two hands.")]
        [SerializeField] protected HoldType holdType = HoldType.MultipleHands;
        [Tooltip("Enable this to unlock the wrist muscles when holding this object.")]
        [SerializeField] internal bool requireAdditionalWristMotion = false;
        [field: SerializeField, Tooltip("Enable this to disable body collision of the object when this grip is held")] public bool IgnoreCollideWithBody { get; private set; } = false;
        [field: SerializeField] public bool UseITCompensation { get; private set; } = true;
        public HandPose Pose
        {
            get => pose;
            set => pose = value;
        }
        
        public virtual Vector3 GetGrabPointEditor(Transform grabber, Transform palm, bool left)
        {
            return grabber.position;
        }

        public virtual Quaternion GetGrabRotationEditor(Transform grabber, Transform palm, bool left)
        {
            return grabber.rotation;
        }
    }
}