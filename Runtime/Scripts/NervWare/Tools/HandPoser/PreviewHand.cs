using System;
using System.Collections.Generic;
using NervBox.Interaction;
using NervBox.Tools.HandPoser;
using UnityEngine;

namespace NervWareSDK.Editor
{
    [ExecuteInEditMode]
    [SelectionBase]
    public class PreviewHand : MonoBehaviour
    {
        [SerializeField] private Transform _palmTransform;
        [SerializeField] private bool _isLeft;
        [SerializeField] protected List<Transform> fingerRoots = new List<Transform>();
        public List<Transform> Joints { get; protected set; } = new List<Transform>();
        private Vector3 _initScale;

        private void Awake()
        {
            Joints = GetJoints();
            _initScale = transform.localScale;
        }

        public void Toggle(bool on)
        {
            transform.localScale = on ? _initScale : Vector3.zero;
        }

        public List<Transform> GetJoints()
        {
            var result = new List<Transform>();
            foreach (var t in fingerRoots)
            {
                foreach (var child in t.GetComponentsInChildren<Transform>())
                {
                    if (!child.name.Contains("Tip"))
                    {
                        result.Add(child);
                    }
                }
            }

            return result;
        }
        public void MoveToGrip(NBGripBase grip)
        {
            Vector3 posDelta = transform.position - _palmTransform.position;
            transform.SetPositionAndRotation(grip.GetGrabPointEditor(this.transform, _palmTransform, _isLeft) + posDelta, 
                grip.GetGrabRotationEditor(transform, _palmTransform, _isLeft));
        }

        public void ApplyPose(HandPose gripPose, bool open)
        {
            HandInfo info = _isLeft ? gripPose.leftHandInfo : gripPose.rightHandInfo;
            ApplyFingerRotations(open ? info.openFingerRotations : info.closedFingerRotations);
        }
        
        public void ApplyFingerRotations(List<Quaternion> rotations)
        {
            if (Joints.Count == 0)
            {
                Joints = GetJoints();
            }
            if (Joints.Count == rotations.Count)
            {
                for (int i = 0; i < Joints.Count; i++)
                {
                    Joints[i].localRotation = rotations[i];
                }
            }
            else
            {
                Debug.Log("joint mismatch! count was " + Joints.Count);
            }
        }
    }
}