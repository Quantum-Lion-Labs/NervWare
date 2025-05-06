using System;
using System.Collections;
using NervBox.Utils;
using SaintsField;
using SaintsField.Playa;
using NervBox.Tools.HandPoser;
using UnityEngine;
using UnityEngine.Serialization;

namespace NervBox.Interaction
{
    public class PointGrip : NBGripBase
    {
        [Tooltip("Can this grip be grabbed upside down?")]
        [SerializeField] private bool allowFlippingUpAxis = false;
        [Tooltip("Can this grip be grabbed backwards?")]
        [SerializeField] private bool allowFlippingForwardAxis = false;
        
        public override Vector3 GetGrabPointEditor(Transform grabber, Transform palm, bool isLeft)
        {
            Transform goal = targetTransform;
            if (goal == null)
            {
                goal = this.transform;
            }

            Vector3 localPos = goal.localPosition;
            if (isLeft && flipHorizontal)
            {
                var pos = goal.localPosition;
                pos.x = -pos.x;
                goal.localPosition = pos;
            }

            Vector3 targetPos = goal.position;

            Vector3 radiusOffset = palm.forward * radius;
            targetPos += -radiusOffset;
            goal.localPosition = localPos;
            return targetPos;
        }

        public override Quaternion GetGrabRotationEditor(Transform grabber, Transform palm, bool isLeft)
        {
            Transform goal = targetTransform;
            if (goal == null)
            {
                goal = this.transform;
            }

            Quaternion localRot = goal.localRotation;
            if (isLeft && flipHorizontal)
            {
                var rot = goal.localRotation;
                rot.y = -rot.y;
                rot.z = -rot.z;
                goal.localRotation = rot;
            }

            Vector3 palmUp = palm.right;
            Vector3 palmForward = palm.up;
            Vector3 targetUp = goal.up;
            Vector3 targetForward = goal.forward;
            if (!isLeft)
            {
                targetUp = -targetUp;
            }

            if (allowFlippingForwardAxis && Vector3.Dot(targetForward, palmForward) < 0f)
            {
                targetForward = -targetForward;
            }

            if (allowFlippingUpAxis && Vector3.Dot(targetUp, palmUp) < 0f)
            {
                targetUp = -targetUp;
            }


            Quaternion solve = Quaternion.LookRotation(targetForward, targetUp);
            Quaternion rotationNeeded = solve * Quaternion.Inverse(Quaternion.LookRotation(palmForward, palmUp));
            Quaternion result = rotationNeeded * grabber.transform.rotation;
            goal.localRotation = localRot;
            return result;
        }
    }
}