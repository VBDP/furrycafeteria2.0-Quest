using UnityEngine;

namespace Cyan.CT.Editor
{
    public class CyanTriggerTypePropertyEditorPlane : CyanTriggerTypePropertyEditorValid
    {
        public CyanTriggerTypePropertyEditorPlane() : base(typeof(Plane), 2) { }

        protected override object DrawProperty(Rect rect, object value, GUIContent content, bool layout)
        {
            return CyanTriggerTypePropertyEditorUtils.DisplayPlaneEditor(rect, (Plane?)value ?? default, content, layout);
        }
    }
}