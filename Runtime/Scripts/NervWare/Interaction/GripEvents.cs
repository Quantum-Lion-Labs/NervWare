using System;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Events;

namespace NervBox.Interaction
{
    public class GripEvents : NetworkBehaviour
    {
        public UnityEvent onAwake = new();
        public UnityEvent onDisable = new();
        public UnityEvent onEnable = new();
        public UnityEvent onAttach = new();
        public UnityEvent onDetach = new();
        public UnityEvent onDestroy = new();
        private NBGripBase _grip;
    }
}