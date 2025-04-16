using System.Collections.Generic;
using Realmsmith.Interaction;
using SaintsField;
using UnityEngine;

namespace Realmsmith.SDK
{
    [DefaultExecutionOrder(short.MinValue - 10)]
    public class ModdedSceneData : MonoBehaviour
    {
        [SerializeField] [ReadOnly] private List<NetworkedInteractable> interactables = new();

        public void PopulateInteractables()
        {
            interactables.Clear();
            interactables.AddRange(FindObjectsByType<NetworkedInteractable>(FindObjectsSortMode.None));
        }
    }
}