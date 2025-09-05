using System.Collections.Generic;
using NervBox.Interaction;
using UnityEngine;

namespace NervBox.SDK
{
    [DefaultExecutionOrder(int.MinValue)]
    public class ModdedSceneData : MonoBehaviour
    {
        [SerializeField]  private List<NetworkedInteractable> interactables = new();
    }
}