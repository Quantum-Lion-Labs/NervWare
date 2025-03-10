#if UNITY_EDITOR
using Realmsmith.Interaction;
using UnityEditor;
using UnityEngine.UIElements;

namespace NervWareSDK.Editor
{
    [CustomEditor(typeof(GenericGrip))]
    public class GenericGripEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement inspector = new VisualElement();
            inspector.Add(new Label("Generic Grip"));
            return inspector;
        }
    }
}
#endif