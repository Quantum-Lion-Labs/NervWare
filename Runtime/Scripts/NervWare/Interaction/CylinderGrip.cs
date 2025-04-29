using System;
using System.Collections;
using System.Collections.Generic;
using SaintsField;
using SaintsField.Playa;
using NervBox.Tools.HandPoser;
using Unity.Mathematics;
using UnityEngine;

namespace NervBox.Interaction
{
    public class CylinderGrip : NBGripBase
    {
        [SerializeField] private float height;
        [SerializeField] private bool allowAnchorUpdating = false;
    }
}