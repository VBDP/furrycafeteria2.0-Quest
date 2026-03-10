using System;
using UnityEngine;

namespace Cyan.CT.Editor
{
    public abstract class CyanTriggerTypePropertyEditorValid : CyanTriggerTypePropertyEditor
    {
        protected CyanTriggerTypePropertyEditorValid(Type type, int lines = 1) : base(type, lines) { }
        
        public override bool HasEditor()
        {
            return true;
        }

        public override object DrawProperty(Rect rect, object value, GUIContent content, bool layout, ref bool heightChanged, string controlHint)
        {
            return DrawProperty(rect, value, content, layout);
        }

        protected virtual object DrawProperty(Rect rect, object value, GUIContent content, bool layout)
        {
            bool heightChanged = false;
            return base.DrawProperty(rect, value, content, layout, ref heightChanged, string.Empty);
        }
    }
}