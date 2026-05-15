using UnityEngine;

namespace Cyan.CT.Editor
{
    public class CyanTriggerTypePropertyEditorVector4 : CyanTriggerTypePropertyEditorValid
    {
        public CyanTriggerTypePropertyEditorVector4() : base(typeof(Vector4)) { }
        
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
            return CyanTriggerTypePropertyEditorUtils.DisplayVector4Editor(rect, (Vector4?) value ?? default, content, layout);
        }
    }
}