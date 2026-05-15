using UnityEngine;

namespace Cyan.CT.Editor
{
    public class CyanTriggerTypePropertyEditorLayerMask : CyanTriggerTypePropertyEditorValid
    {
        public CyanTriggerTypePropertyEditorLayerMask() : base(typeof(LayerMask)) { }

        protected override object DrawProperty(Rect rect, object value, GUIContent content, bool layout)
        {
            return CyanTriggerTypePropertyEditorUtils.DisplayLayerMaskEditor(rect, (LayerMask?) value ?? default, content, layout);
        }
    }
}