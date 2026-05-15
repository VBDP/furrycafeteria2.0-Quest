using System;
using UnityEditor;
using UnityEngine;

namespace Cyan.CT.Editor
{
    public class CyanTriggerTypePropertyEditor
    {
        public static readonly CyanTriggerTypePropertyEditor InvalidEditor = new CyanTriggerTypePropertyEditor(null);
        
        protected readonly Type Type;
        private readonly int _lines;

        protected CyanTriggerTypePropertyEditor(Type type, int lines = 1)
        {
            Type = type;
            _lines = lines;
        }

        public virtual bool HasEditor()
        {
            return false;
        }

        public virtual bool IsSingleLine()
        {
            return _lines == 1;
        }

        public virtual float GetPropertyHeight(object value, string controlHint)
        {
            return GetHeightFromLines(_lines);
        }

        public virtual object DrawProperty(Rect rect, object value, GUIContent content, bool layout, ref bool heightChanged, string controlHint)
        {
            CyanTriggerTypePropertyEditorUtils.DisplayMissingEditor(rect, value, layout);
            
            return value;
        }

        public Type GetPropertyType()
        {
            return Type;
        }

        private static float GetHeightFromLines(int lines)
        {
            return EditorGUIUtility.singleLineHeight * lines + ((lines - 1) * 2);
        }
    }
}