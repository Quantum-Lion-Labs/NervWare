using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using NervBox.Interaction;
using NervWareSDK;
using Unity.Mathematics;
using UnityEngine;

namespace NervBox.Combat
{
    public class NBCollisionSender : NetworkBehaviour
    {
        [SerializeField] [Tooltip("Multiplier for collision damage.")]
        [FieldGroup("Damager Settings")]
        public float hitImpactMultiplier = 1f;
    }
}