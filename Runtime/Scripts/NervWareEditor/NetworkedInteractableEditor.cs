#if UNITY_EDITOR
using NervBox.Editor;
using NervBox.Interaction;
using UnityEditor;

namespace NervWareSDK.Editor
{
    [CustomEditor(typeof(NetworkedInteractable))]
    [CanEditMultipleObjects]
    public class NetworkedInteractableEditor : NervWareStyledEditor
    {
        protected override void InitializeProperties()
        {
        }

        protected override string GetInspectorName()
        {
            return "NETWORKED INTERACTABLE";
        }

        protected override void DrawInspector()
        {
        }
    }
}
#endif