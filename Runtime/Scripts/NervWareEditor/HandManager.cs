using System;
using NervBox.Interaction;
using NervBox.Tools.HandPoser;
using UnityEngine;

namespace NervWareSDK.Editor
{
    [ExecuteInEditMode]
    public class HandManager : MonoBehaviour
    {
        public PreviewHand LeftHand;
        public PreviewHand RightHand;
        private HandPose _gripPose;
        private NBGripBase _grip;
        private bool _open;
        private void OnDestroy()
        {
            DestroyImmediate(LeftHand.gameObject);
            DestroyImmediate(RightHand.gameObject);
        }

        private void Update()
        {
            if (!_grip || !_gripPose) return;
            LeftHand.MoveToGrip(_grip);
            RightHand.MoveToGrip(_grip);
            LeftHand.ApplyPose(_gripPose, _open);
            RightHand.ApplyPose(_gripPose, _open);
        }

        public void UpdateHands(HandPose gripPose, NBGripBase grip, bool open)
        {
            _grip = grip;
            _gripPose = gripPose;
            _open = open;
            // transform.SetParent(grip.transform, true);
            // LeftHand.transform.SetParent(grip.transform, true);
            // RightHand.transform.SetParent(grip.transform, true);
            LeftHand.MoveToGrip(grip);
            RightHand.MoveToGrip(grip);
            LeftHand.ApplyPose(gripPose, open);
            RightHand.ApplyPose(gripPose, open);
        }
    }
}