using UnityEngine;

namespace Cyan.CT.Editor
{
    public class CyanTriggerTypePropertyEditorRay : CyanTriggerTypePropertyEditorValid
    {
        public CyanTriggerTypePropertyEditorRay() : base(typeof(Ray), 2) { }

        protected override object DrawProperty(Rect rect, object value, GUIContent content, bool layout)
        {
            return CyanTriggerTypePropertyEditorUtils.DisplayRayEditor(rect, (Ray?)value ?? default, content, layout);
        }
    }
}