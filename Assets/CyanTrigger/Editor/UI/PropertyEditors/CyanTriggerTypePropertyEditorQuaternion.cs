
using UnityEngine;

namespace Cyan.CT.Editor
{
    public class CyanTriggerTypePropertyEditorQuaternion : CyanTriggerTypePropertyEditorValid
    {
        public CyanTriggerTypePropertyEditorQuaternion() : base(typeof(Quaternion)) { }

        public override bool IsSingleLine()
        {
            return false;
        }

        public override float GetPropertyHeight(object value, string controlHint)
        {
            return base.GetPropertyHeight(value, controlHint) + 1;
        }

        protected override object DrawProperty(Rect rect, object value, GUIContent content, bool layout)
        {
            return CyanTriggerTypePropertyEditorUtils.DisplayQuaternionEditor(rect, (Quaternion?) value ?? default, content, layout);
        }
    }
}