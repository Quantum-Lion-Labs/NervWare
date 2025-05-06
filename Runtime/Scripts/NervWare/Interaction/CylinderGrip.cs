using System;
using System.Collections;
using System.Collections.Generic;
using SaintsField;
using SaintsField.Playa;
using NervBox.Tools.HandPoser;
using NervBox.Utils;
using Unity.Mathematics;
using UnityEngine;


namespace NervBox.Interaction
{
    public class CylinderGrip : NBGripBase
    {
        [Tooltip("The height of the cylinder, in world space.")]
        [SerializeField] private float height;
        [SerializeField] private bool allowAnchorUpdating = false;
        private void OnDrawGizmosSelected()
        {
            if (radius < 0f || height <= 0f) return;
            SimpleTransform t = new SimpleTransform(targetTransform ? targetTransform : transform);
            DrawCylinder(t.Position + t.Up * (-height * 0.5f), t.Rotation, height, radius,
                Color.white);
        }

        public override Vector3 GetGrabPointEditor(Transform grabber, Transform palm, bool isLeft)
        {
            Transform goal = targetTransform;
            if (goal == null)
            {
                goal = transform;
            }

            Vector3 localPos = goal.localPosition;
            if (isLeft && flipHorizontal)
            {
                var pos = goal.localPosition;
                pos.x = -pos.x;
                goal.localPosition = pos;
            }

            Vector3 upAxis = goal.up;
            Vector3 goalPos = goal.position;
            Vector3 bottomPosition = goalPos + upAxis * (-height * 0.5f);
            Vector3 topPosition = goalPos + upAxis * (height * 0.5f);
            Vector3 targetPos =
                FindNearestPointOnLine(bottomPosition, topPosition, palm.position);
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
            Vector3 goalPos = goal.position;
            Vector3 bottomPosition = goalPos + targetUp * (-height * 0.5f);
            Vector3 topPosition = goalPos + targetUp * (height * 0.5f);
            Vector3 axis = (bottomPosition - topPosition).normalized;

            Vector3 cross = Vector3.Cross(axis, palmForward);
            Vector3 cross2 = Vector3.Cross(cross, axis);
            if (Vector3.Dot(axis, palmUp) < 0)
            {
                axis = -axis;
            }

            if (Vector3.Dot(cross2, palmForward) < 0)
            {
                cross2 = -cross2;
            }

            Quaternion rotationNeeded = Quaternion.LookRotation(cross2, axis) *
                                        Quaternion.Inverse(Quaternion.LookRotation(palmForward, palmUp));
            Quaternion result = rotationNeeded * grabber.transform.rotation;
            goal.localRotation = localRot;
            return result;
        }
        
        private static Vector3 FindNearestPointOnLine(Vector3 origin, Vector3 end, Vector3 point)
        {
            var heading = (end - origin);
            float magnitudeMax = heading.magnitude;
            heading.Normalize();

            var lhs = point - origin;
            float dotP = Vector3.Dot(lhs, heading);
            dotP = Mathf.Clamp(dotP, 0f, magnitudeMax);
            return origin + heading * dotP;
        }

        private static void DrawCylinder(Vector3 position, Quaternion orientation, float height, float radius, Color color, bool drawFromBase = true)
        {
            Vector3 localUp = orientation * Vector3.up;
            Vector3 localRight = orientation * Vector3.right;
            Vector3 localForward = orientation * Vector3.forward;
 
            Vector3 basePositionOffset = drawFromBase ? Vector3.zero : (localUp * height * 0.5f);
            Vector3 basePosition = position - basePositionOffset;
            Vector3 topPosition = basePosition + localUp * height;
         
            Quaternion circleOrientation = orientation * Quaternion.Euler(90, 0, 0);
 
            Vector3 pointA = basePosition + localRight * radius;
            Vector3 pointB = basePosition + localForward * radius;
            Vector3 pointC = basePosition - localRight * radius;
            Vector3 pointD = basePosition - localForward * radius;
            Gizmos.color = color;
            Gizmos.DrawRay(pointA, localUp * height);
            Gizmos.DrawRay(pointB, localUp * height);
            Gizmos.DrawRay(pointC, localUp * height);
            Gizmos.DrawRay(pointD, localUp * height);
 
 
            DrawCircle(basePosition, circleOrientation, radius, 32, color);
            DrawCircle(topPosition, circleOrientation, radius, 32, color);
        }
        
        private static void DrawCircle(Vector3 position, Quaternion rotation, float radius, int segments, Color color)
        {
            if (radius <= 0.0f || segments <= 0)
            {
                return;
            }
 
            float angleStep = (360.0f / segments);
 
 
            angleStep *= Mathf.Deg2Rad;
 
            Vector3 lineStart = Vector3.zero;
            Vector3 lineEnd = Vector3.zero;
 
            for (int i = 0; i < segments; i++)
            {
                lineStart.x = Mathf.Cos(angleStep * i);
                lineStart.y = Mathf.Sin(angleStep * i);
                lineStart.z = 0.0f;
 
                lineEnd.x = Mathf.Cos(angleStep * (i + 1));
                lineEnd.y = Mathf.Sin(angleStep * (i + 1));
                lineEnd.z = 0.0f;
 
                lineStart *= radius;
                lineEnd *= radius;
 
                lineStart = rotation * lineStart;
                lineEnd = rotation * lineEnd;
 
                lineStart += position;
                lineEnd += position;
 
                Gizmos.color = color;
                Gizmos.DrawLine(lineStart, lineEnd);
            }
        }
    }
    
}