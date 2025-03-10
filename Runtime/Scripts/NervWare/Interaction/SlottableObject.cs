using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FishNet;
using FishNet.Object;
using SaintsField;
using SaintsField.Playa;
using UnityEngine;

namespace Realmsmith.Interaction
{
    public enum SlotType
    {
        Large,
        Small,
        Head,
        None
    }

    public class SlottableObject : NetworkBehaviour
    {
        [LayoutStart("Slottable Object References", ELayout.TitleBox)]
        [Required("A grip is required!")]
        [Tooltip("The grip required to be held to slot the item.")]
        [SerializeField]
        private ASGripBase primaryGrip;

        [Required("The start point must be defined.")]
        [SerializeField] [Tooltip("The start point of the length of the object.")]
        private Transform startPoint;

        [Required("The end point must be defined.")]
        [BelowButton(nameof(AddPoints), "Add start and end points")]
        [SerializeField] [Tooltip("The end point of the length of the object.")]
        private Transform endPoint;

       
        [LayoutEnd]
        [LayoutStart("Slottable Object Settings", ELayout.TitleBox)]
        [Tooltip("The size of the body slot.")] [SerializeField]
        private SlotType slotType;

        [Tooltip("Where along the item it should be positioned.")] [field: SerializeField] [Range(0f, 1f)]
        private float targetPoint = 0.5f;

        [Tooltip("Whether the object needs to be flipped on the main axis of the object.")] [SerializeField]
        private bool flipAxis = false;
        
        [ValidateInput(nameof(ValidateAxis))]
        [Tooltip("The secondary axis of the object. It should be perpendicular to the main axis.")] [SerializeField]
        private Vector3 secondaryAxis = Vector3.right;

        private void AddPoints()
        {
            startPoint = new GameObject("StartPoint").transform;
            startPoint.parent = transform;
            startPoint.localPosition = Vector3.zero;
            startPoint.localRotation = Quaternion.identity;

            endPoint = new GameObject("EndPoint").transform;
            endPoint.parent = transform;
            endPoint.localPosition = Vector3.zero;
            endPoint.localRotation = Quaternion.identity;
        }

        private void OnDrawGizmosSelected()
        {
            if (startPoint == null || endPoint == null) return;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(startPoint.position, endPoint.position);
            Gizmos.DrawWireSphere(startPoint.position, 0.025f);
            Gizmos.DrawWireSphere(endPoint.position, 0.025f);
            Gizmos.DrawWireSphere(GetTargetPoint(), 0.025f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(GetTargetPoint(), GetTargetPoint() + GetSecondaryAxis() * 0.15f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + GetTargetAxis() * 0.15f);
        }

        private string ValidateAxis()
        {
            return secondaryAxis == Vector3.zero ? "Axis cannot be zero!" : null;
        }

        public Vector3 GetTargetPoint()
        {
            return Vector3.Lerp(startPoint.position, endPoint.position, targetPoint);
        }

        public Vector3 GetTargetAxis()
        {
            if (flipAxis)
            {
                return (startPoint.position - endPoint.position).normalized;
            }

            return (endPoint.position - startPoint.position).normalized;
        }

        public Vector3 GetSecondaryAxis()
        {
            return transform.rotation * secondaryAxis;
        }
    }
}