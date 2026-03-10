using UnityEngine;

namespace Cyan.CT.Editor
{
    public class CyanTriggerTypePropertyEditorBool : CyanTriggerTypePropertyEditorValid
    {
        public CyanTriggerTypePropertyEditorBool() : base(typeof(bool)) { }

        protected override object DrawProperty(Rect rect, object value, GUIContent content, bool layout)
        {
            return CyanTriggerTypePropertyEditorUtils.DisplayBoolEditor(rect, (bool?) value ?? default, content, layout);
        }
    }
}