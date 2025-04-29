using System.Collections.Generic;
using SaintsField;
using SaintsField.Playa;
using UnityEngine;

namespace NervBox.Tools.HandPoser
{
    public enum HandType
    {
        Left,
        Right,
        None
    }

    [CreateAssetMenu(fileName = "NewHandPose", menuName = "ScriptableObjects/Hand Pose/Hand Pose Data")]
    public partial class HandPose : ScriptableObject
    {
        public HandInfo leftHandInfo = HandInfo.Empty;
        public HandInfo rightHandInfo = HandInfo.Empty;
    }

    [System.Serializable]
    public partial class HandInfo
    {
        public List<Quaternion> openFingerRotations = new List<Quaternion>();
        public List<Quaternion> closedFingerRotations = new List<Quaternion>();
        public static HandInfo Empty => new HandInfo();
        public Vector3 controllerPosOffset = Vector3.zero;
        public Vector3 eulerOffset = Vector3.zero;
        public Quaternion ControllerRotOffset => Quaternion.Euler(eulerOffset);        
    }
}