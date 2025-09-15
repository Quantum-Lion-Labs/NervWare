using NervBox.Interaction;
using UnityEditor;
using UnityEngine;

namespace NervWareSDK.Editor
{
    [CustomEditor(typeof(GripEvents))]
    public class GripEventsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var gripEvents = target as GripEvents;
            if (gripEvents != null && gripEvents.gameObject.GetComponentInParent<NetworkedInteractable>() == null)
            {
                EditorGUILayout.HelpBox(
                    "A NetworkedInteractable is required! Please add one to this object or a parent object.",
                    MessageType.Error);
            }

            base.OnInspectorGUI();
        }
    }
}