using System;
using UnityEditor;
using UnityEngine;

namespace Cyan.CT.Editor
{
    public class CyanTriggerTypePropertyEditorType : CyanTriggerTypePropertyEditorValid
    {
        public CyanTriggerTypePropertyEditorType() : base(typeof(Type)) { }

        private string _pressedControl;
        private Type _selectedType;
        
        public override object DrawProperty(Rect rect, object value, GUIContent content, bool layout, ref bool heightChanged, string controlHint)
        {
            Type typeValue = (Type)value;
            string tooltip = "null";
            string typename = "null";
            if (typeValue != null)
            {
                typename = CyanTriggerNameHelpers.GetTypeFriendlyName(typeValue);
                tooltip = typeValue.FullName;
            }

            GUIContent typeContent = new GUIContent(typename, tooltip);
            
            if (layout)
            {
                rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(EditorGUIUtility.singleLineHeight));
                GUILayout.Space(1);

                Rect labelWidth = new Rect(rect) { width = EditorGUIUtility.labelWidth };
                EditorGUI.LabelField(labelWidth, content);
                
                rect.xMin += EditorGUIUtility.labelWidth;
            }
            
            bool pressed = GUI.Button(rect, typeContent, EditorStyles.popup);
            
            if (layout)
            {
                EditorGUILayout.EndHorizontal();
            }
            
            if (pressed)
            {
                CyanTriggerSearchWindowManager.Instance.DisplayVariableSearchWindow(
                    definition => OnTypeSelected(definition?.type, controlHint), true, false);
            }

            // Changes only save on the repaint event and not layout.
            if (Event.current?.type == EventType.Repaint && _pressedControl == controlHint)
            {
                value = _selectedType;
                _pressedControl = null;
                _selectedType = null;
                GUI.changed = true;
            }
            
            return value;
        }

        private void OnTypeSelected(Type type, string id)
        {
            _pressedControl = id;
            _selectedType = type;
        }
    }
}