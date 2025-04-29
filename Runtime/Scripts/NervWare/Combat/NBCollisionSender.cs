using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using NervBox.Interaction;
using SaintsField;
using Unity.Mathematics;
using UnityEngine;

namespace NervBox.Combat
{
    public class NBCollisionSender : NetworkBehaviour
    {
        [SerializeField] [SepTitle("Damager Settings", EColor.Aqua)] [Tooltip("Multiplier for collision damage.")]
        public float hitImpactMultiplier = 1f;
    }
}