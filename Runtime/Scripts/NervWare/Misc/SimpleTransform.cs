using System;
using UnityEngine;


namespace NervBox.Utils
{
    [Serializable]
    public struct SimpleTransform
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;

        public SimpleTransform(Vector3 pos, Quaternion rot)
        {
            Position = pos;
            Rotation = rot;
            Velocity = Vector3.zero;
            AngularVelocity = Vector3.zero;
        }

        public SimpleTransform(Transform t)
        {
            t.GetPositionAndRotation(out Position, out Rotation);
            Velocity = Vector3.zero;
            AngularVelocity = Vector3.zero;
        }
        
        public SimpleTransform(SimpleTransform t)
        {
            Position = t.Position;
            Rotation = t.Rotation;
            Velocity = t.Velocity;
            AngularVelocity = t.AngularVelocity;
        }

        public SimpleTransform(Rigidbody rb)
        {
            Position = rb.position;
            Rotation = rb.rotation;
            Velocity = rb.linearVelocity;
            AngularVelocity = rb.angularVelocity;
        }

        public void StoreFromTransform(Transform t)
        {
            t.GetPositionAndRotation(out Position, out Rotation);
        }

        public void SendToTransform(Transform t)
        {
            t.SetPositionAndRotation(Position, Rotation);
        }

        public void StoreFromOther(SimpleTransform t)
        {
            Position = t.Position;
            Rotation = t.Rotation;
        }

        public Vector3 Right => Rotation * Vector3.right;
        public Vector3 Up => Rotation * Vector3.up;
        public Vector3 Forward => Rotation * Vector3.forward;

        public Vector3 TransformDirection(Vector3 localDirection)
        {
            return Rotation * localDirection;
        }

        public Vector3 InverseTransformDirection(Vector3 worldDirection)
        {
            return Quaternion.Inverse(Rotation) * worldDirection;
        }

        public Vector3 TransformPoint(Vector3 localPos)
        {
            return Position + TransformDirection(localPos);
        }

        public Vector3 InverseTransformPoint(Vector3 worldPos)
        {
            return InverseTransformDirection(worldPos - Position);
        }

        public Quaternion TransformRotation(Quaternion localRot)
        {
            return Rotation * localRot;
        }

        public Quaternion InverseTransformRotation(Quaternion worldRot)
        {
            return Quaternion.Inverse(Rotation) * worldRot;
        }
        
        public SimpleTransform Transform(SimpleTransform transform)
        {
            return new SimpleTransform(TransformPoint(transform.Position), TransformRotation(transform.Rotation));
        }
        
        public SimpleTransform InverseTransform(SimpleTransform transform) 
        {
            return new SimpleTransform(InverseTransformPoint(transform.Position), InverseTransformRotation(transform.Rotation));
        }

        public new string ToString()
        {
            return
                $"Position: {Position}, Rotation: {Rotation}, Velocity: {Velocity}, AngularVelocity: {AngularVelocity}";
        }

        public SimpleTransform Lerp(SimpleTransform tr, float t)
        {
            return new SimpleTransform(Vector3.Lerp(Position, tr.Position, t),
                Quaternion.Slerp(Rotation, tr.Rotation, t));
        }

        public static SimpleTransform Lerp(SimpleTransform tr, SimpleTransform tr2, float t)
        {
            return new SimpleTransform(Vector3.Lerp(tr.Position, tr2.Position, t),
                Quaternion.Lerp(tr.Rotation, tr2.Rotation, t));
        }

        public Matrix4x4 ToMatrix(float scale)
        {
            if (Rotation.w == 0)
            {
                Rotation = Quaternion.identity;
            }
            return Matrix4x4.TRS(Position, Rotation, Vector3.one * scale);
        }
    }
}