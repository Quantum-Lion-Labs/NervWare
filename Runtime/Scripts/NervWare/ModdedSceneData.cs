using System.Collections.Generic;
using NervBox.Interaction;
using UnityEngine;

namespace NervBox.SDK
{
    [DefaultExecutionOrder(short.MinValue - 10)]
    public class ModdedSceneData : MonoBehaviour
    {
        [SerializeField]  private List<NetworkedInteractable> interactables = new();

        public void PopulateInteractables()
        {
            interactables.Clear();
            interactables.AddRange(FindObjectsByType<NetworkedInteractable>(FindObjectsSortMode.None));
        }
    }
}