using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace Cyan.CT.Editor
{
    public static class CyanTriggerTypePropertyEditorUtils
    {
        public static void DisplayMissingEditor(Rect rect, object value, bool layout)
        {
            string display = "null";
            if (value != null)
            {
                display = value.ToString();
            }
            GUIContent content = new GUIContent(display, $"Value = {display}\nNo defined editor. Consider using a constructor to create an instance of this type.");
            DisplayLabel(rect, content, layout);
        }
        
        public static void DisplayLabel(Rect rect, GUIContent content, bool layout)
        {
            if (layout)
            {
                EditorGUILayout.LabelField(content);
            }
            else
            {
                EditorGUI.LabelField(rect, content);
            }
        }
        
        public static bool DisplayBoolEditor(Rect rect, bool value, GUIContent symbol, bool layout)
        {
            float toggleWidth;
            
            if (layout)
            {
                value = EditorGUILayout.Toggle(symbol, value);
                rect = GUILayoutUtility.GetLastRect();
                toggleWidth = EditorGUIUtility.labelWidth + 20;
            }
            else
            {
                rect.xMin += 1;
                Rect toggleRect = new Rect(rect);
                toggleWidth = GUI.skin.label.CalcSize(symbol).x + 15;
                toggleRect.width = toggleWidth;
                value = EditorGUI.Toggle(toggleRect, symbol, value);
            }
            
            // Draw True/False next to the toggle to make it obvious what it is.
            if (!EditorGUI.showMixedValue)
            {
                Rect labelRect = new Rect(rect);
                labelRect.width = rect.width - toggleWidth;
                labelRect.x += toggleWidth;
                EditorGUI.LabelField(labelRect, value.ToString());
            }
            
            return value;
        }
        
        public static float DisplayFloatEditor(Rect rect, float floatValue, GUIContent symbol, bool layout)
        {
            if (layout)
            {
                floatValue = EditorGUILayout.FloatField(symbol, floatValue);
            }
            else
            {
                floatValue = EditorGUI.FloatField(rect, symbol, floatValue);
            }

            return floatValue;
        }

        public static Vector3 DisplayVector3Editor(Rect rect, Vector3 vector3Value, GUIContent symbol, bool layout)
        {
            if (layout)
            {
                vector3Value = EditorGUILayout.Vector3Field(symbol, vector3Value);
            }
            else
            {
                vector3Value = EditorGUI.Vector3Field(rect, symbol, vector3Value);
            }

            return vector3Value;
        }
        
        public static Vector4 DisplayVector4Editor(Rect rect, Vector4 vector4Value, GUIContent symbol, bool layout)
        {
            if (layout)
            {
                vector4Value = EditorGUILayout.Vector4Field(symbol, vector4Value);
            }
            else
            {
                vector4Value = EditorGUI.Vector4Field(rect, symbol, vector4Value);
            }

            return vector4Value;
        }

        private static Vector3 _cachedQuaternionEulerVector;
        public static Quaternion DisplayQuaternionEditor(Rect rect, Quaternion quaternionValue, GUIContent symbol, bool layout)
        {
            int controlIndex = GUIUtility.GetControlID(FocusType.Passive);

            // Vector3 property editors create 4 controls. Check if current control is within this range.
            bool IsCurrentControl()
            {
                return controlIndex < GUIUtility.keyboardControl && 
                       GUIUtility.keyboardControl <= controlIndex + 4;
            }

            Vector3 euler = quaternionValue.eulerAngles;
            if (IsCurrentControl())
            {
                euler = _cachedQuaternionEulerVector;
            }

            euler = DisplayVector3Editor(rect, euler, symbol, layout);
            quaternionValue = Quaternion.Euler(euler);

            if (IsCurrentControl())
            {
                _cachedQuaternionEulerVector = euler;
            }
            
            return quaternionValue;
        }
        
        public static Ray DisplayRayEditor(Rect rect, Ray ray, GUIContent symbol, bool layout)
        {
            var width = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Min(width, rect.width / 3);

            Vector3 origin = ray.origin;
            Vector3 direction = ray.direction;
            
            EditorGUI.BeginChangeCheck();
            
            rect.height *= 0.5f;
            origin = DisplayVector3Editor(rect, origin, new GUIContent("Origin"), layout);
            rect.y += rect.height;
            direction = DisplayVector3Editor(rect, direction, new GUIContent("Direction"), layout);
           
            if (EditorGUI.EndChangeCheck())
            {
                ray = new Ray(origin, direction);
            }

            EditorGUIUtility.labelWidth = width;
            return ray;
        }
        
        public static Plane DisplayPlaneEditor(Rect rect, Plane plane, GUIContent symbol, bool layout)
        {
            var width = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Min(width, rect.width / 3);

            Vector3 normal = plane.normal;
            float distance = plane.distance;
            
            EditorGUI.BeginChangeCheck();
            
            rect.height *= 0.5f;
            normal = DisplayVector3Editor(rect, normal, new GUIContent("Normal"), layout);
            rect.y += rect.height;
            distance = DisplayFloatEditor(rect, distance, new GUIContent("Distance"), layout);
            
            if (EditorGUI.EndChangeCheck())
            {
                plane = new Plane(normal, distance);
            }

            EditorGUIUtility.labelWidth = width;
            return plane;
        }
        
        
        private static readonly float[] Matrix4Rows = new float[4];
        private static readonly GUIContent[][] Matrix4RowLabels =
        {
            new[] { new GUIContent(" 0"), new GUIContent(" 1"), new GUIContent(" 2"), new GUIContent(" 3") },
            new[] { new GUIContent(" 4"), new GUIContent(" 5"), new GUIContent(" 6"), new GUIContent(" 7") },
            new[] { new GUIContent(" 8"), new GUIContent(" 9"), new GUIContent("10"), new GUIContent("11") },
            new[] { new GUIContent("12"), new GUIContent("13"), new GUIContent("14"), new GUIContent("15") },
        };
        
        public static Matrix4x4 DisplayMatrix4X4Editor(Rect rect, Matrix4x4 matrixValue, GUIContent symbol, bool layout)
        {
            if (layout)
            {
                float tempLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 16;

                for (int row = 0; row < 4; ++row)
                {
                    EditorGUILayout.BeginHorizontal();

                    for (int col = 0; col < 4; ++col)
                    {
                        int index = row * 4 + col;
                        matrixValue[index] = EditorGUILayout.FloatField(index.ToString(), matrixValue[index]);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUIUtility.labelWidth = tempLabelWidth;
            }
            else
            {
                float height = rect.height * 0.25f;
                for (int row = 0; row < 4; ++row)
                {
                    Rect rowRect = new Rect(
                        rect.x,
                        rect.y + height * row,
                        rect.width,
                        height
                    );
                    
                    Matrix4Rows[0] = matrixValue[row + 0];
                    Matrix4Rows[1] = matrixValue[row + 4];
                    Matrix4Rows[2] = matrixValue[row + 8];
                    Matrix4Rows[3] = matrixValue[row + 12];
                    
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.MultiFloatField(rowRect, Matrix4RowLabels[row], Matrix4Rows);
                    if (EditorGUI.EndChangeCheck())
                    {
                        matrixValue[row + 0] = Matrix4Rows[0];
                        matrixValue[row + 4] = Matrix4Rows[1];
                        matrixValue[row + 8] = Matrix4Rows[2];
                        matrixValue[row + 12] = Matrix4Rows[3];
                    }
                }
            }

            return matrixValue;
        }
        
        public static LayerMask DisplayLayerMaskEditor(Rect rect, LayerMask layerMaskValue, GUIContent symbol, bool layout)
        {
            if (layout)
            {
                layerMaskValue = EditorGUILayout.MaskField(symbol, layerMaskValue, UnityEditorInternal.InternalEditorUtility.layers);
            }
            else
            {
                layerMaskValue = EditorGUI.MaskField(rect, symbol, layerMaskValue, UnityEditorInternal.InternalEditorUtility.layers);
            }

            return layerMaskValue;
        }

        public static VRCPlayerApi DisplayPlayerEditor(Rect rect, VRCPlayerApi player, GUIContent symbol, bool layout)
        {
            if (!EditorApplication.isPlaying)
            {
                DisplayMissingEditor(rect, player, layout);
                return player;
            }

            List<(GUIContent, object)> playerList = new List<(GUIContent, object)>();
            foreach (var curPlayer in VRCPlayerApi.AllPlayers)
            {
                playerList.Add((new GUIContent($"({curPlayer.playerId}) {curPlayer.displayName}"), curPlayer));
            }

            int selectedId = player == null ? -1 : VRCPlayerApi.GetPlayerId(player);
            bool FindSelectedPlayer(object other)
            {
                if (!(other is VRCPlayerApi otherPlayer))
                {
                    return false;
                }
                return selectedId == otherPlayer.playerId;
            }
            
            return (VRCPlayerApi)DisplayListSelector(typeof(VRCPlayerApi), rect, symbol, player, layout, playerList, FindSelectedPlayer);
        }
        
        public static object DisplayListSelector(
            Type type,
            Rect rect,
            GUIContent symbol,
            object data,
            bool layout,
            List<(GUIContent, object)> items,
            Func<object, bool> selectionCompare = null)
        {
            if (selectionCompare == null)
            {
                object compareData = data;
                selectionCompare = ((obj) => (obj == null && compareData == null) || (obj != null && obj.Equals(compareData)));
            }
            
            int curSelected = 0;
            GUIContent[] displayNames = new GUIContent[items.Count + 1];
            displayNames[0] = new GUIContent("-");
            for (int i = 0; i < items.Count; ++i)
            {
                var obj = items[i];
                object curData = obj.Item2;
                if (selectionCompare(curData))
                {
                    curSelected = i + 1;
                }
                displayNames[i + 1] = obj.Item1;
            }

            // Ensure empty selection resets to default value and does not leave previous.
            if (curSelected == 0)
            {
                bool dirty = false;
                object value = CyanTriggerPropertyEditor.ResetToDefaultValue(type, data, ref dirty);
                if (dirty)
                {
                    data = value;
                    GUI.changed = true;
                }
            }

            int selected;
            if (layout)
            {
                selected = EditorGUILayout.Popup(symbol, curSelected, displayNames);
            }
            else
            {
                selected = EditorGUI.Popup(rect, symbol, curSelected, displayNames);
            }
            
            if (selected != curSelected)
            {
                data = selected != 0 ? items[selected-1].Item2 : CyanTriggerPropertyEditor.GetDefaultForType(type);
            }

            return data;
        }
    }
}