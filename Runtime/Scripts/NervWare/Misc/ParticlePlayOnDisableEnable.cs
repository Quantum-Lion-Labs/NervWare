using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NervBox.Misc
{
    /// <summary>
    /// Plays the assigned serialized particle system whenever the gameObject is activated.
    /// Stops when the gameObject is deactivated.
    /// </summary>
    public class ParticlePlayOnDisableEnable : MonoBehaviour
    {
        [SerializeField] private ParticleSystem system;
        private void OnEnable()
        {
            system.Play(true);
        }

        private void OnDisable()
        {
            system.Stop(true);
        }
    }
}