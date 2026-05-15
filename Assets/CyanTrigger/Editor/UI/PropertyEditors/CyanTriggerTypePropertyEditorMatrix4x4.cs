using UnityEngine;

namespace Cyan.CT.Editor
{
    public class CyanTriggerTypePropertyEditorMatrix4x4 : CyanTriggerTypePropertyEditorValid
    {
        public CyanTriggerTypePropertyEditorMatrix4x4() : base(typeof(Matrix4x4), 4) { }

        protected override object DrawProperty(Rect rect, object value, GUIContent content, bool layout)
        {
            return CyanTriggerTypePropertyEditorUtils.DisplayMatrix4X4Editor(rect, (Matrix4x4?) value ?? default, content, layout);
        }
    }
}