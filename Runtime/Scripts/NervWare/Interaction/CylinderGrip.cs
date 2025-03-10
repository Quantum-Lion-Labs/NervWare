using System;
using System.Collections;
using System.Collections.Generic;
using SaintsField;
using SaintsField.Playa;
using Realmsmith.Tools.HandPoser;
using Unity.Mathematics;
using UnityEngine;

namespace Realmsmith.Interaction
{
    public class CylinderGrip : ASGripBase
    {
        [SerializeField] private float height;
        [SerializeField] private bool allowAnchorUpdating = false;
    }
}