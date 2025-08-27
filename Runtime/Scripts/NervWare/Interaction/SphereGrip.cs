
using UnityEngine;

namespace NervBox.Interaction
{
    public class SphereGrip : NBGripBase
    {
        private SphereCollider sphereCollider;
        private Quaternion? _lastRotationL, _lastRotationR;
        private float _lastRadius = 0f;

        public override void OnDisable()
        {
            _lastRotationL = null;
            _lastRotationR = null;
        }

        private void OnDrawGizmos()
        {
            sphereCollider = GetComponentInChildren<SphereCollider>();
            Transform tr = targetTransform ? targetTransform : transform;
            if (sphereCollider != null)
            {
                ToWorldSpaceSphere(sphereCollider, out var center, out var radius);
                Gizmos.DrawWireSphere(tr.position, radius);
                return;
            }

            if (radius <= 0f)
            {
                return;
            }

            Gizmos.DrawWireSphere(tr.position, radius);
        }

        public override Vector3 GetGrabPointEditor(Transform grabber, Transform palm, bool isLeft)
        {
            float sphRadius = radius;
            if (sphereCollider)
            {
                ToWorldSpaceSphere(sphereCollider, out Vector3 v0, out sphRadius);
            }

            Transform tr = targetTransform ? targetTransform : transform;
            Vector3 center = tr.position;
            Vector3 dir = palm.position - center;
            dir.Normalize();
            dir *= sphRadius;
            Vector3 result = center + dir;
            return result;
        }


        public override Quaternion GetGrabRotationEditor(Transform grabber, Transform palm, bool isLeft)
        {
            float sphRadius = radius;
            if (sphereCollider)
            {
                ToWorldSpaceSphere(sphereCollider, out Vector3 v0, out sphRadius);
            }

            if (!Mathf.Approximately(_lastRadius, sphRadius))
            {
                _lastRotationL = null;
                _lastRotationR = null;
                _lastRadius = sphRadius;
            }

            if (isLeft && _lastRotationL.HasValue)
            {
                return _lastRotationL.Value;
            }

            if (!isLeft && _lastRotationR.HasValue)
            {
                return _lastRotationR.Value;
            }

            Quaternion result = Quaternion.identity;
            for (int i = 0; i < 4; i++)
            {
                Transform tr = targetTransform ? targetTransform : transform;
                Vector3 center = tr.position;
                Vector3 dir = palm.position - center;
                result = Quaternion.LookRotation(dir, palm.up)
                             * Quaternion.Euler(0, isLeft ? 90f : -90f, 0);
            }

            if (isLeft)
            {
                _lastRotationL = result;
            }
            else
            {
                _lastRotationR = result;
            }

            return result;
        }

        private static void ToWorldSpaceSphere(SphereCollider sphere, out Vector3 center, out float radius)
        {
            center = sphere.transform.TransformPoint(sphere.center);
            radius = sphere.radius * MaxVec3(AbsVec3(sphere.transform.lossyScale));
        }

        private static Vector3 AbsVec3(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        private static float MaxVec3(Vector3 v)
        {
            return Mathf.Max(v.x, Mathf.Max(v.y, v.z));
        }
    }
}