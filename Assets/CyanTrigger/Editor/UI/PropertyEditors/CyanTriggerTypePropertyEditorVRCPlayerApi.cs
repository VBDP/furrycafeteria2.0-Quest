using UnityEngine;
using VRC.SDKBase;

namespace Cyan.CT.Editor
{
    public class CyanTriggerTypePropertyEditorVRCPlayerApi : CyanTriggerTypePropertyEditorValid
    {
        public CyanTriggerTypePropertyEditorVRCPlayerApi() : base(typeof(VRCPlayerApi)) { }

        protected override object DrawProperty(Rect rect, object value, GUIContent content, bool layout)
        {
            return CyanTriggerTypePropertyEditorUtils.DisplayPlayerEditor(rect, (VRCPlayerApi)value, content, layout);
        }
    }
}