#if UNITY_EDITOR
using NervBox.Combat;
using NervBox.Editor;
using UnityEditor;

namespace NervWareSDK
{
    [CustomEditor(typeof(NBCollisionSender))]
    public class NBCollisionSenderEditor : NervWareStyledEditor
    {
        protected override void InitializeProperties()
        {
            
        }

        protected override string GetInspectorName()
        {
            return "COLLISION SENDER";
        }

        protected override void DrawInspector()
        {
            
        }
    }
}
#endif