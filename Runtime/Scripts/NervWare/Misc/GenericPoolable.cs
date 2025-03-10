using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Realmsmith.Misc
{
    public class GenericPoolable : MonoBehaviour, IPoolable
    {
        [SerializeField] private bool disableOnReturn;
        [SerializeField] private UnityEvent onDisable;
        [SerializeField] private UnityEvent onEnable;

        public void WarmUp(int id)
        {
            
        }

        public void Return()
        {
         
        }

        public void OnActivate()
        {
         
        }

        public void OnRelease()
        {
         
        }

        public bool DisableOnReturn { get; }
        public bool DisableBeforeReuse { get; }
        public GameObject GetGameObject()
        {
            return null;
        }

        public Transform GetTransform()
        {
            return null;
        }
    }
}