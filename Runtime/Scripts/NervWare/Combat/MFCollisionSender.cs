using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using Realmsmith.Interaction;
using SaintsField;
using Unity.Mathematics;
using UnityEngine;

namespace Realmsmith.Combat
{
    public class MFCollisionSender : NetworkBehaviour
    {
        [SerializeField] [SepTitle("Damager Settings", EColor.Aqua)] [Tooltip("Multiplier for collision damage.")]
        public float hitImpactMultiplier = 1f;
    }
}