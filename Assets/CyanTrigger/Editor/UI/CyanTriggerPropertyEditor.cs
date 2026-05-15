using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.Udon.Common.Interfaces;
using Object = UnityEngine.Object;

namespace Cyan.CT.Editor
{
    public static class CyanTriggerPropertyEditor
    {
        private const float FoldoutListHeaderHeight = 16;
        private const float FoldoutListHeaderAreaHeight = 19;
        
        private static GUIStyle _footerButtonStyle;
        private static GUIStyle _footerBackgroundStyle;
        private static GUIStyle _headerBackgroundStyle;

        private static readonly MethodInfo SetBoldDefaultFont = 
            typeof(EditorGUIUtility).GetMethod("SetBoldDefaultFont", BindingFlags.Static | BindingFlags.NonPublic);
        
        private static void SetBoldFont(bool bold)
        {
            SetBoldDefaultFont.Invoke(null, new[] { bold as object });
        }
        
        public static bool DrawEditor(
            SerializedProperty dataProperty,
            Rect rect,
            GUIContent variableName,
            Type type,
            ref bool heightChanged,
            string controlHint,
            bool layout = false,
            Func<List<(GUIContent, object)>> getConstInputOptionsFunc = null)
        {
            bool multi = EditorGUI.showMixedValue;
            bool shouldShowMixed = dataProperty.hasMultipleDifferentValues;
            
            if (!layout)
            {
                EditorGUI.BeginProperty(rect, GUIContent.none, dataProperty);
            }
            else if (dataProperty.isInstantiatedPrefab)
            {
                EditorGUI.showMixedValue = shouldShowMixed;
                SetBoldFont(dataProperty.prefabOverride);
            }

            // Prevent getting the data on layout events and only use default value for type.
            bool isLayoutEvent = Event.current?.type == EventType.Layout;
            object obj = isLayoutEvent || (shouldShowMixed && typeof(Object).IsAssignableFrom(type))
                ? GetDefaultForType(type)
                : CyanTriggerSerializableObject.ObjectFromSerializedProperty(dataProperty);
            
            bool dirty = false;
            obj = DisplayPropertyEditor(rect, variableName, type, obj, ref dirty, ref heightChanged, controlHint, layout, getConstInputOptionsFunc);

            if (!isLayoutEvent && dirty)
            {
                CyanTriggerSerializableObject.UpdateSerializedProperty(dataProperty, obj);
            }
            
            if (!layout)
            {
                EditorGUI.EndProperty();
            }
            else
            {
                SetBoldFont(false);
                EditorGUI.showMixedValue = multi;
            }

            return dirty;
        }

        public static bool DrawArrayEditor(
            SerializedProperty dataProperty, 
            GUIContent variableName, 
            Type type, 
            ref bool arrayExpand, 
            Action onHeightChanged,
            ref ReorderableList list, 
            string controlHint,
            bool layout = true, 
            Rect rect = default)
        {
            bool multi = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = dataProperty.hasMultipleDifferentValues;
            
            if (!layout)
            {
                EditorGUI.BeginProperty(rect, GUIContent.none, dataProperty);
            }
            else if (dataProperty.isInstantiatedPrefab)
            {
                SetBoldFont(dataProperty.prefabOverride);
            }
            
            object obj = CyanTriggerSerializableObject.ObjectFromSerializedProperty(dataProperty);
            bool dirty = false;
            obj = DisplayArrayPropertyEditor(variableName, type, obj, ref dirty, ref arrayExpand, onHeightChanged, ref list, controlHint, layout, rect);

            if (dirty)
            {
                Type elementType = type.GetElementType();
                if(typeof(Object).IsAssignableFrom(elementType))
                {
                    var array = (Array) obj;
                    
                    // ReSharper disable once AssignNullToNotNullAttribute
                    Array destinationArray = Array.CreateInstance(elementType, array.Length);
                    Array.Copy(array, destinationArray, array.Length);
                
                    obj = destinationArray;
                }
                
                CyanTriggerSerializableObject.UpdateSerializedProperty(dataProperty, obj);
            }
            
            if (!layout)
            {
                EditorGUI.EndProperty();
            }
            else
            {
                SetBoldFont(false);
            }
            
            EditorGUI.showMixedValue = multi;
            
            return dirty;
        }

        /*
        public static bool DrawEditor(ref SerializableObject serializedObject, Rect rect, string variableName, Type type, ref bool arrayExpand, ref ReorderableList list)
        {
            bool dirty = false;
            object obj = DisplayPropertyEditor(rect, new GUIContent(variableName), type, serializedObject.obj, ref dirty, ref arrayExpand, ref list);

            if (dirty)
            {
                serializedObject.obj = obj;
            }

            return dirty;
        }
        */

        public static bool TypeHasSingleLineEditor(Type type)
        {
            if (type.IsArray)
            {
                return false;
            }

            var editor = CyanTriggerTypePropertyEditorManager.GetPropertyEditor(type);
            if (editor.HasEditor())
            {
                return editor.IsSingleLine();
            }

            // TODO error in displaying editor
            return true;
        }
        
        public static bool TypeHasInLineEditor(Type type)
        {
            return !type.IsArray;
        }
        
        public static float HeightForInLineEditor(Type variableType, object variableValue, string controlHint, bool addMultilineExtras)
        {
            if (TypeHasSingleLineEditor(variableType))
            {
                return EditorGUIUtility.singleLineHeight;
            }

            if (!variableType.IsArray)
            {
                var editor = CyanTriggerTypePropertyEditorManager.GetPropertyEditor(variableType);
                if (editor.HasEditor())
                {
                    float height = editor.GetPropertyHeight(variableValue, controlHint);
                    if (addMultilineExtras && height > EditorGUIUtility.singleLineHeight)
                    {
                        height += EditorGUIUtility.singleLineHeight;
                    }
                    return height;
                }
                
                throw new NotSupportedException($"Cannot calculate line height for type: {variableType}");
            }

            throw new NotSupportedException($"Array types are not supported in line: {variableType}");
        }
        
        // TODO make a better api that doesn't take a list...
        public static float HeightForEditor(
            Type variableType, 
            object variableValue, 
            bool showList, 
            ref ReorderableList list,
            Action onHeightChanged, 
            string controlHint)
        {
            if (!variableType.IsArray)
            {
                return HeightForInLineEditor(variableType, variableValue, controlHint, false);
            }

            float height = FoldoutListHeaderAreaHeight;
            if (showList)
            {
                Type elementType = variableType.GetElementType();
                CreateReorderableListForVariable(elementType, variableValue as Array, ref list, onHeightChanged, controlHint);
                height += list.GetHeight();
            }

            return height;
        }

        public static object CreateInitialValueForType(Type type, object variableValue, ref bool dirty)
        {
            // Check is required for the case when a destroyed object is still saved, but shouldn't be. 
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (typeof(Object).IsAssignableFrom(type) 
                && variableValue != null 
                && (!(variableValue is Object valueObj) || valueObj == null))
            {
                dirty = true;
                return null;
            }
            
            if(!type.IsInstanceOfType(variableValue)
               // Prevent it throwing errors because string class is null instead of empty string
               && !(ClassTypesWithInitialValues.Contains(type) && variableValue == null))
            {
                object value = GetDefaultForType(type);
                if (value != variableValue)
                {
                    dirty = true;
                    variableValue = value;
                }
            }

            return variableValue;
        }
        
        public static object ResetToDefaultValue(Type type, object value, ref bool dirty)
        {
            object originalValue = value;
            value = CreateInitialValueForType(type, value, ref dirty);
            if (dirty)
            {
                return value;
            }

            if(type.IsValueType)
            {
                object defaultValue = GetDefaultForType(type);
                if (!defaultValue.Equals(value))
                {
                    dirty = true;
                }
                return defaultValue;
            }
            if (type.IsArray)
            {
                if (!(value is Array array) || array.Length != 0)
                {
                    value = GetDefaultForType(type);
                    dirty = true;
                }
                return value;
            }
            if (type == typeof(Gradient) && value == null)
            {
                dirty = true;
                return new Gradient();
            }

            if (value != null && originalValue != value)
            {
                dirty = true;
            }
            
            return null;
        }

        private static readonly HashSet<Type> ClassTypesWithInitialValues = new HashSet<Type>()
        {
            typeof(string),
            typeof(Gradient),
        };
        
        public static object GetDefaultForType(Type type)
        {
            #region Initialize specific type defaults

            if (type == typeof(Color))
            {
                return new Color(1, 1, 1, 1);
            }
            if (type == typeof(Color32))
            {
                return new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
            }
            if (type == typeof(string))
            {
                return "";
            }
            if (type == typeof(Gradient))
            {
                return new Gradient();
            }
            // TODO if updating this list with a nullable type (like string), update ClassTypesWithInitialValues

            #endregion
            
            
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            if (type.IsArray)
            {
                return Array.CreateInstance(type.GetElementType(), 0);
            }

            return null;
        }
        
        public static object DisplayPropertyEditor(
            Rect rect, 
            GUIContent content, 
            Type variableType, 
            object variableValue, 
            ref bool dirty, 
            ref bool heightChanged,
            string controlHint,
            bool layout = false,
            Func<List<(GUIContent, object)>> getConstInputOptions = null)
        {
            if (variableType.IsArray)
            {
                Debug.LogWarning("Trying to display an array type using the object method!");
                return variableValue;
            }

            variableValue = CreateInitialValueForType(variableType, variableValue, ref dirty);

            if (layout)
            {
                EditorGUILayout.BeginHorizontal();
            }

            EditorGUI.BeginChangeCheck();

            var list = getConstInputOptions?.Invoke();
            if (list != null)
            {
                variableValue = CyanTriggerTypePropertyEditorUtils.DisplayListSelector(variableType, rect, content, variableValue, layout, list);
            }
            else
            {
                var editor = CyanTriggerTypePropertyEditorManager.GetPropertyEditor(variableType);
                variableValue = editor.DrawProperty(rect, variableValue, content, layout, ref heightChanged, controlHint);
            }

            if (layout)
            {
                EditorGUILayout.EndHorizontal();
            }

            if(EditorGUI.EndChangeCheck())
            {
                dirty = true;
            }

            return variableValue;
        }

        public static object DisplayArrayPropertyEditor(
            GUIContent variableName, 
            Type variableType, 
            object variableValue, 
            ref bool dirty, 
            ref bool showList, 
            Action onHeightChanged,
            ref ReorderableList list,
            string controlHint,
            bool layout = true,
            Rect rect = default)
        {
            if (!variableType.IsArray)
            {
                Debug.LogWarning("Trying to display a non array type using the array method!");
                return variableValue;
            }

            Type elementType = variableType.GetElementType();

            if (variableValue == null)
            {
                variableValue = Array.CreateInstance(elementType, 0);
                dirty = true;
            }

            return DisplayArrayPropertyEditor(
                elementType, 
                variableValue as Array, 
                ref dirty,
                ref showList,
                onHeightChanged,
                variableName,
                ref list,
                layout,
                rect,
                controlHint);
        }

        public static void CreateReorderableListForVariable(
            Type variableType,
            Array variableValue,
            ref ReorderableList list,
            Action onHeightChanged,
            string controlHint)
        {
            if (list != null)
            {
                return;
            }

            string ControlHintPerIndex(int index)
            {
                return $"{controlHint}-{index}";
            }
            
            ReorderableList listInstance = list = new ReorderableList(
                variableValue, 
                variableType, 
                true, 
                false,
                true,
                true);
            list.headerHeight = 0;
            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                // Remove the 2 pixel padding
                rect.y += 1;
                rect.height -= 2;
                
                bool changed = false;
                bool heightChanged = false;
                listInstance.list[index] = DisplayPropertyEditor(
                    rect,
                    GUIContent.none,
                    variableType,
                    listInstance.list[index],
                    ref changed,
                    ref heightChanged,
                    ControlHintPerIndex(index));

                if (heightChanged)
                {
                    onHeightChanged?.Invoke();
                }
            };
            list.onAddCallback = reorderableList =>
            {
                int length = reorderableList.list.Count;
                Array values = Array.CreateInstance(variableType, length + 1);
                for (int i = 0; i < length; ++i)
                {
                    values.SetValue(reorderableList.list[i], i);
                }
                values.SetValue(GetDefaultForType(variableType), length);

                reorderableList.list = values;
            };
            list.onRemoveCallback = reorderableList =>
            {
                int selected = reorderableList.index;
                int length = reorderableList.list.Count;
                Array values = Array.CreateInstance(variableType, length - 1);

                int selectedFound = 0;
                for (int i = 0; i < values.Length; ++i)
                {
                    if (i == selected)
                    {
                        selectedFound = 1;
                    }
                    values.SetValue(reorderableList.list[i + selectedFound], i);
                }
                reorderableList.list = values;
            };
            list.elementHeightCallback = index =>
            {
                float height = HeightForInLineEditor(variableType, listInstance.list?[index], ControlHintPerIndex(index), false);
                
                // Unity 2022 adds a 2 pixel padding, which looks good. Add this back to 2019 version.
#if !UNITY_2022_3_OR_NEWER
                height += 2;
#endif
                
                return height;
            };
            list.onReorderCallback = reorderableList => onHeightChanged?.Invoke();
            
            // Since a new list has been created, assume the height has changed as well.
            onHeightChanged?.Invoke();
        }
        
        public static Array DisplayArrayPropertyEditor(
            Type variableType,
            Array variableValue,
            ref bool dirty,
            ref bool showList,
            Action onHeightChanged,
            GUIContent content,
            ref ReorderableList list,
            bool layout,
            Rect rect,
            string controlHint)
        {
            if (layout)
            {
                EditorGUILayout.BeginVertical();
            }

            ReorderableList reorderableList = list;
            bool sizeChange = false;
            DrawFoldoutListHeader(
                content,
                ref showList,
                true,
                variableValue.Length,
                size =>
                {
                    if (reorderableList == null)
                    {
                        return;
                    }

                    Array values = Array.CreateInstance(variableType, size);
                    for (int i = 0; i < size; ++i)
                    {
                        values.SetValue(i < reorderableList.list.Count ? reorderableList.list[i] : GetDefaultForType(variableType), i);
                    }

                    reorderableList.list = values;
                    sizeChange = true;
                },
                // Only allow drag for GameObject or Component fields
                typeof(GameObject).IsAssignableFrom(variableType) ||
                 typeof(Component).IsAssignableFrom(variableType) ||
                 typeof(IUdonEventReceiver).IsAssignableFrom(variableType),
                dragObjects =>
                {
                    if (reorderableList == null)
                    {
                        return;
                    }
                    
                    List<Object> objects = GetGameObjectsOrComponentsFromDraggedObjects(dragObjects, variableType);

                    if (objects.Count == 0)
                    {
                        return;
                    }
                    
                    int startSize = reorderableList.list.Count;
                    int size = startSize + objects.Count;
                    Array values = Array.CreateInstance(variableType, size);
                    for (int i = 0; i < reorderableList.list.Count; ++i)
                    {
                        values.SetValue(reorderableList.list[i], i);
                    }
                    for (int i = 0; i < objects.Count; ++i)
                    {
                        values.SetValue(objects[i], startSize + i);
                    }

                    reorderableList.list = values;
                },
                false, 
                true, 
                layout,
                rect);

            if (!showList)
            {
                if (layout)
                {
                    EditorGUILayout.EndVertical();
                }
                return variableValue;
            }

            if (sizeChange && list != null)
            {
                variableValue = (Array)list.list;
                dirty = true;
                onHeightChanged?.Invoke();
            }
            
            // Check if the data has changed outside of the editor and update the list.
            // This should only happen in playmode through udon.
            if (Application.isPlaying
                && list != null 
                && !CyanTriggerDataReferences.DeepEquals(list.list, variableValue))
            {
                list.list = variableValue;
                dirty = true;
                onHeightChanged?.Invoke();
            }
            
            if (list == null)
            {
                CreateReorderableListForVariable(variableType, variableValue, ref list, onHeightChanged, controlHint);
            }
            
            EditorGUI.BeginChangeCheck();

            if (layout)
            {
                list.DoLayoutList();
            }
            else
            {
                rect.y += FoldoutListHeaderHeight + 2;
                rect.height -= FoldoutListHeaderHeight + 2;
                list.DoList(rect);
            }

            bool allEqual = list.count == variableValue.Length;
            for (int i = 0; allEqual && i < list.count && i < variableValue.Length; ++i)
            {
                allEqual &= list.list[i] != variableValue.GetValue(i);
            }
        
            if (
                EditorGUI.EndChangeCheck() ||
                list.count != variableValue.Length ||
                !allEqual
            )
            {
                if (variableValue.Length != list.count)
                {
                    variableValue = Array.CreateInstance(variableType, list.count);
                }

                for (int i = 0; i < variableValue.Length; ++i)
                {
                    variableValue.SetValue(list.list[i], i);
                }
                
                dirty = true;
            }
            
            if (layout)
            {
                EditorGUILayout.EndVertical();
            }

            return variableValue;
        }

        public static List<Object> GetGameObjectsOrComponentsFromDraggedObjects(Object[] dragObjects, Type type)
        {
            List<Object> objects = new List<Object>();
            bool isGameObject = typeof(GameObject).IsAssignableFrom(type);
            bool isComponent = typeof(Component).IsAssignableFrom(type) ||
                               typeof(IUdonEventReceiver).IsAssignableFrom(type);
                    
            for (int i = 0; i < dragObjects.Length; ++i)
            {
                var obj = dragObjects[i];
                if (isGameObject)
                {
                    if (obj is GameObject gameObject)
                    {
                        objects.Add(gameObject);
                    }
                    else if (obj is Component component)
                    {
                        objects.Add(component.gameObject);
                    }
                }
                else if (isComponent)
                {
                    if (obj is Component component)
                    {
                        objects.Add(component);
                    }
                    else if (obj is GameObject gameObject)
                    {
                        var components = gameObject.GetComponents(type);
                        if (components.Length > 0)
                        {
                            objects.AddRange(components);
                        }
                    }
                }
            }

            return objects;
        }
        
        public static void DrawFoldoutListHeader(
            GUIContent content,
            ref bool visibilityState,
            bool showSizeEditor,
            int currentSize,
            Action<int> onSizeChanged,
            bool allowItemDrag,
            Action<Object[]> onItemDragged,
            bool showError = false,
            bool showHeaderBackground = true,
            bool layout = true,
            Rect rect = default,
            string documentationTooltip = null,
            string documentationLink = null)
        {
            Rect foldMainRect = rect;
            if (layout)
            {
                foldMainRect = EditorGUILayout.BeginHorizontal();
            }
            
            Rect foldoutRect = new Rect(foldMainRect.x + 17, foldMainRect.y + 1, foldMainRect.width - 18, FoldoutListHeaderHeight);
            Rect header = new Rect(foldMainRect);
            header.height = foldoutRect.height + 4;
            if (showHeaderBackground && Event.current.type == EventType.Repaint)
            {
                if (_headerBackgroundStyle == null)
                {
                    _headerBackgroundStyle = "RL Header";
                }
                _headerBackgroundStyle.Draw(header, false, false, false, false);
            }
            
            Rect sizeRect = new Rect(foldoutRect);
            float separatorSize = 6;
            float maxSizeWidth = 75;

            bool showDocumentationButtons =
                !string.IsNullOrEmpty(documentationTooltip) && !string.IsNullOrEmpty(documentationLink);
            if (showDocumentationButtons)
            {
                foldoutRect.width -= 20;
            }
            
            if (visibilityState && showSizeEditor)
            {
                foldoutRect.width -= maxSizeWidth - separatorSize;
            }

            if (showError)
            {
                content.image = EditorGUIUtility.FindTexture("Error");
            }
            
            // Check dragged objects before foldout as it will become "used" after
            Event evt = Event.current;
            if (allowItemDrag &&
                visibilityState && 
                header.Contains(evt.mousePosition))
            {
                if (evt.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                }
                if (evt.type == EventType.DragPerform)
                {
                    Object[] dragObjects = DragAndDrop.objectReferences.ToArray();
                    onItemDragged?.Invoke(dragObjects);
                    DragAndDrop.AcceptDrag();
                    evt.Use();
                }
            }
            
            CyanTriggerNameHelpers.TruncateContent(content, foldoutRect);
            bool show = EditorGUI.Foldout(foldoutRect, visibilityState, content, true);
            // Just clicked the arrow, unfocus any elements, which could have been the size component
            if (!show && visibilityState)
            {
                GUI.FocusControl(null);
            }
            visibilityState = show;

            if (visibilityState && showSizeEditor)
            {
                sizeRect.y += 1;
                sizeRect.height -= 1;
                sizeRect.width = maxSizeWidth;
                sizeRect.x += foldoutRect.width - separatorSize;

                float prevWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 30;
                
                int size = EditorGUI.IntField(sizeRect, "Size", currentSize);
                size = Math.Max(0, size);
                
                EditorGUIUtility.labelWidth = prevWidth;

                if (size != currentSize)
                {
                    onSizeChanged?.Invoke(size);
                }
            }
            
            if (showDocumentationButtons)
            {
                Rect docRect = new Rect(foldoutRect.xMax + 4, foldoutRect.y + 1, 16, 16);
                CyanTriggerEditorUtils.DrawDocumentationButton(docRect, documentationTooltip, documentationLink);
            }
            
            if (layout)
            {
                EditorGUILayout.EndHorizontal();
                float offset = 0;
#if !UNITY_2019_4_OR_NEWER
                offset = -3;
#endif
                GUILayout.Space(header.height + offset);
            }
        }

        public static void DrawButtonFooter(
            GUIContent[] icons, 
            Action[] buttons, 
            bool[] shouldDisable, 
            string documentationTooltip = null,
            string documentationLink = null)
        {
            if (_footerButtonStyle == null)
            {
                _footerButtonStyle = "RL FooterButton";
                _footerBackgroundStyle = "RL Footer";
            }
            
            
            Rect footerRect = EditorGUILayout.BeginHorizontal();
            float xMax = footerRect.xMax;
#if UNITY_2019_4_OR_NEWER
            xMax -= 8;
            footerRect.height = 16;
#else
            footerRect.height = 11;
#endif
            float x = xMax - 8f;
            const float buttonWidth = 25;
            x -= buttonWidth * icons.Length;
            footerRect = new Rect(x, footerRect.y, xMax - x, footerRect.height);
                    
            if (Event.current.type == EventType.Repaint)
            {
                _footerBackgroundStyle.Draw(footerRect, false, false, false, false);
            }

#if !UNITY_2019_4_OR_NEWER
            footerRect.y -= 3f;
#endif
            
            for (int i = 0; i < icons.Length; ++i)
            {
                Rect buttonRect = new Rect(x + 4f + buttonWidth * i, footerRect.y, buttonWidth, 13f);
                
                EditorGUI.BeginDisabledGroup(shouldDisable[i]);
                
                GUIStyle style = _footerButtonStyle;
                if (icons[i].image == null)
                {
                    style = new GUIStyle { alignment = TextAnchor.LowerCenter, fontSize = 8};
                    style.normal.textColor = GUI.skin.label.normal.textColor;
                }
                if (GUI.Button(buttonRect, icons[i], style))
                {
                    buttons[i]?.Invoke();
                }
                EditorGUI.EndDisabledGroup();
            }
                    
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(documentationTooltip) && !string.IsNullOrEmpty(documentationLink))
            {
                Rect docRect = new Rect(x - 20, footerRect.y, 16, 16);
                CyanTriggerEditorUtils.DrawDocumentationButton(docRect, documentationTooltip, documentationLink);
            }
            
            GUILayout.Space(footerRect.height + 4);
        }

        public static Rect DrawErrorIcon(Rect rect, string reason)
        {
            GUIContent errorIcon = EditorGUIUtility.TrIconContent("CollabError", reason);
            Rect errorRect = new Rect(rect);
            float iconWidth = 15;
            float spaceBetween = 1;
            errorRect.width = iconWidth;
            errorRect.y += 3;
                
            EditorGUI.LabelField(errorRect, errorIcon);
                
            rect.x += iconWidth + spaceBetween;
            rect.width -= iconWidth + spaceBetween;

            return rect;
        }

        public static void InitializeMultiInputEditors(
            CyanTriggerActionInstanceRenderData actionInstanceRenderData,
            Func<Type, List<CyanTriggerEditorVariableOption>> getVariableOptionsForType)
        {
            var actionProperty = actionInstanceRenderData.Property;
            var variableDefinitions = actionInstanceRenderData.VariableDefinitions;

            // No variables, no need to initialize anything.
            if (variableDefinitions.Length == 0)
            {
                return;
            }
            
            CyanTriggerActionVariableDefinition variableDefinition = variableDefinitions[0];
            
            // Not a multi-editor, no need to initialize.
            if ((variableDefinition.variableType & CyanTriggerActionVariableTypeDefinition.AllowsMultiple) == 0)
            {
                return;
            }
            
            var multiInputListProperty = 
                actionProperty.FindPropertyRelative(nameof(CyanTriggerActionInstance.multiInput));
                    
            CreateActionVariableInstanceMultiInputEditor(
                actionInstanceRenderData, 
                0, 
                multiInputListProperty,
                variableDefinition, 
                getVariableOptionsForType);
        }

        public static float GetHeightForActionInstanceInputEditors(
            CyanTriggerActionInstanceRenderData actionInstanceRenderData,
            bool checkCustomHeight = true)
        {
            var actionProperty = actionInstanceRenderData.Property;
            var inputListProperty = actionProperty.FindPropertyRelative(nameof(CyanTriggerActionInstance.inputs));

            var variableDefinitions = actionInstanceRenderData.VariableDefinitions;
            if (inputListProperty.arraySize != variableDefinitions.Length)
            {
                Debug.LogWarning($"Improper variable input size! {inputListProperty.arraySize} != {variableDefinitions.Length}");
                inputListProperty.arraySize = variableDefinitions.Length;
            }
            
            // Custom Height Implementation
            if (checkCustomHeight
                && CyanTriggerCustomNodeInspectorManager.Instance.TryGetCustomInspector(
                    actionInstanceRenderData.ActionInfo,
                    out var customNodeInspector)
                && customNodeInspector.HasCustomHeight(actionInstanceRenderData))
            {
                return customNodeInspector.GetHeightForInspector(actionInstanceRenderData);
            }
            
            float height = 0;
            int visibleCount = 0;
            for (int curInput = 0; curInput < variableDefinitions.Length; ++curInput)
            {
                CyanTriggerActionVariableDefinition variableDefinition = variableDefinitions[curInput];
                if (variableDefinition == null)
                {
                    continue;
                }

                if ((variableDefinition.variableType & CyanTriggerActionVariableTypeDefinition.Hidden) != 0)
                {
                    continue;
                }
                ++visibleCount;
                
                // First option is a multi input editor
                if (curInput == 0 &&
                    (variableDefinition.variableType & CyanTriggerActionVariableTypeDefinition.AllowsMultiple) != 0)
                {
                    height += GetHeightForActionVariableInstanceMultiInputEditor(
                        variableDefinition.type.Type,
                        actionInstanceRenderData.ExpandedInputs[curInput],
                        actionInstanceRenderData.InputLists[curInput]);
                }
                else
                {
                    SerializedProperty inputProperty = inputListProperty.GetArrayElementAtIndex(curInput);
                    height += GetHeightForActionVariableInstanceInputEditor(
                        variableDefinition,
                        inputProperty,
                        actionInstanceRenderData.ExpandedInputs[curInput],
                        ref actionInstanceRenderData.InputLists[curInput],
                        () => actionInstanceRenderData.NeedsRedraws = true,
                        actionInstanceRenderData.ControlHint);
                }
            }

            return height + Mathf.Max(0, 5 * (visibleCount + 1));
        }

        public static float GetHeightForActionVariableInstanceMultiInputEditor(
            Type propertyType,
            bool expandList,
            ReorderableList list)
        {
            if (!expandList)
            {
                return FoldoutListHeaderAreaHeight;
            }
            
            bool displayEditorInLine = TypeHasInLineEditor(propertyType);
            return FoldoutListHeaderAreaHeight
                   + list.GetHeight()
                   + (displayEditorInLine ? 0 : EditorGUIUtility.singleLineHeight + 5);
        }

        private static float GetHeightForActionVariableInstanceInputEditor(
            CyanTriggerActionVariableDefinition variableDefinition,
            SerializedProperty inputProperty,
            bool expandList,
            ref ReorderableList list,
            Action onHeightChanged,
            string controlHint)
        {
            SerializedProperty isVariableProperty =
                inputProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.isVariable));
            bool isVariable = isVariableProperty.boolValue;

            Type propertyType = variableDefinition.type.Type;
            object data = null;
            SerializedProperty dataProperty = null;
            
            // Only collect the data when it is expected to be used. 
            if (!isVariable)
            {
                dataProperty = inputProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.data));
                data = CyanTriggerSerializableObject.ObjectFromSerializedProperty(dataProperty);
            }
            
            float height = GetHeightForActionInputInLineEditor(variableDefinition, isVariable, data, controlHint);
            
            // input based array editors are dependent on the size of the array.
            if (propertyType.IsArray && !isVariable)
            {
                // Initialize type if missing
                bool dirty = false;
                object updatedValue = CreateInitialValueForType(propertyType, data, ref dirty);
                if (dirty)
                {
                    data = updatedValue;
                    CyanTriggerSerializableObject.UpdateSerializedProperty(dataProperty, data);
                }
                
                height += 5;
                height += HeightForEditor(
                    propertyType,
                    data,
                    expandList,
                    ref list,
                    onHeightChanged,
                    controlHint);
            }

            return height;
        }

        public static void DrawActionInstanceInputEditors(
            CyanTriggerActionInstanceRenderData actionInstanceRenderData,
            Func<Type, List<CyanTriggerEditorVariableOption>> getVariableOptionsForType,
            Rect rect = default,
            bool layout = false,
            Action<HashSet<string>, HashSet<string>> onVariableDeleted = null)
        {
            actionInstanceRenderData.ContainsNull = false;
            
            var actionProperty = actionInstanceRenderData.Property;
            var variableDefinitions = actionInstanceRenderData.VariableDefinitions;
            var inputListProperty = actionProperty.FindPropertyRelative(nameof(CyanTriggerActionInstance.inputs));

            if (inputListProperty.arraySize != variableDefinitions.Length)
            {
                inputListProperty.arraySize = variableDefinitions.Length;
            }
            
            if (actionInstanceRenderData.ExpandedInputs.Length != variableDefinitions.Length)
            {
                actionInstanceRenderData.UpdateVariableSize();
            }
            
            // Draw custom inspectors
            if (CyanTriggerCustomNodeInspectorManager.Instance.TryGetCustomInspector(
                    actionInstanceRenderData.ActionInfo,
                    out var customNodeInspector))
            {
                customNodeInspector.RenderInspector(
                    actionInstanceRenderData, 
                    variableDefinitions, 
                    getVariableOptionsForType, 
                    rect, 
                    layout);
                return;
            }

            bool shouldCheckOutputVariables = onVariableDeleted != null;
            HashSet<string> removedVariables = new HashSet<string>();
            
            for (int curInput = 0; curInput < variableDefinitions.Length; ++curInput)
            {
                CyanTriggerActionVariableDefinition variableDefinition = variableDefinitions[curInput];
                
                Rect inputRect = new Rect(rect);
                
                // First option is a multi input editor
                if (curInput == 0 &&
                    (variableDefinition.variableType & CyanTriggerActionVariableTypeDefinition.AllowsMultiple) != 0)
                {
                    var multiInputListProperty = 
                        actionProperty.FindPropertyRelative(nameof(CyanTriggerActionInstance.multiInput));
                    
                    // TODO check for array of output variables if any are removed.
                    
                    DrawActionVariableInstanceMultiInputEditor(
                        actionInstanceRenderData,
                        curInput,
                        multiInputListProperty, 
                        variableDefinition,
                        getVariableOptionsForType,
                        ref inputRect,
                        layout);
                }
                else
                {
                    SerializedProperty inputProperty = inputListProperty.GetArrayElementAtIndex(curInput);
                    
                    // Get old variable guid to know if a new output variable is removed.
                    SerializedProperty guidProp = null;
                    string oldGuid = null;
                    bool isOutput = (variableDefinition.variableType &
                                     CyanTriggerActionVariableTypeDefinition.VariableOutput) != 0;
                    if (shouldCheckOutputVariables && isOutput)
                    {
                        SerializedProperty nameProp = inputProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.name));
                        guidProp = inputProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.variableID));
                        
                        string varName = nameProp.stringValue;
                        oldGuid = guidProp.stringValue;
                        
                        // We know we have a valid output variable set here
                        isOutput = string.IsNullOrEmpty(varName) && !string.IsNullOrEmpty(oldGuid);
                    }
                    
                    
                    DrawActionVariableInstanceInputEditor(
                        actionInstanceRenderData,
                        curInput,
                        inputProperty, 
                        variableDefinition,
                        getVariableOptionsForType, 
                        ref inputRect,
                        layout);
                    
                    
                    // Old guid does not match new guid. Output variable was removed.
                    if (shouldCheckOutputVariables && isOutput && oldGuid != guidProp.stringValue)
                    {
                        removedVariables.Add(oldGuid);
                    }
                }

                // Only update rect size when variable is not hidden.
                if ((variableDefinition.variableType & CyanTriggerActionVariableTypeDefinition.Hidden) == 0)
                {
                    rect.y += inputRect.height + 5;
                    rect.height -= inputRect.height + 5;
                }
            }

            if (shouldCheckOutputVariables && removedVariables.Count > 0)
            {
                onVariableDeleted(removedVariables, new HashSet<string>());
            }
        }

        public static void DrawActionVariableInstanceInputEditor(
            CyanTriggerActionInstanceRenderData actionInstanceRenderData,
            int inputIndex,
            SerializedProperty variableProperty,
            CyanTriggerActionVariableDefinition variableDefinition,
            Func<Type, List<CyanTriggerEditorVariableOption>> getVariableOptionsForType,
            ref Rect rect,
            bool layout = false,
            Func<List<(GUIContent, object)>> getConstInputOptions = null)
        {
            if (variableDefinition == null)
            {
                return;
            }
            Type propertyType = variableDefinition.type.Type;

            GUIContent variableDisplayName =
                new GUIContent(variableDefinition.displayName, variableDefinition.description);
            
            rect.height = GetHeightForActionVariableInstanceInputEditor(
                variableDefinition,
                variableProperty,
                actionInstanceRenderData.ExpandedInputs[inputIndex],
                ref actionInstanceRenderData.InputLists[inputIndex],
                () => actionInstanceRenderData.NeedsRedraws = true,
                actionInstanceRenderData.ControlHint);
            
            Rect inputRect = new Rect(rect);
            
            // Skip hidden input, but set default value
            if ((variableDefinition.variableType & CyanTriggerActionVariableTypeDefinition.Hidden) != 0)
            {
                SerializedProperty dataProperty =
                    variableProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.data));
                
                // For hidden variables, assume that this is creating a new variable with the specified name and a new guid.
                if ((variableDefinition.variableType & CyanTriggerActionVariableTypeDefinition.VariableOutput) != 0)
                {
                    // TODO create helper method for creating variables and reuse here and in general out property editors
                    SerializedProperty idProperty =
                        variableProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.variableID));
                    if (string.IsNullOrEmpty(idProperty.stringValue))
                    {
                        idProperty.stringValue = Guid.NewGuid().ToString();
                    }
                    CyanTriggerSerializableObject.UpdateSerializedProperty(dataProperty, variableDefinition.displayName);
                }
                // Not a new variable, just use default value.
                else
                {
                    CyanTriggerSerializableObject.UpdateSerializedProperty(dataProperty, variableDefinition.defaultValue?.Obj);
                }
                
                return;
            }

            if (layout)
            {
                EditorGUILayout.Space();
                inputRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.Space();
            }

            SerializedProperty isVariableProperty =
                variableProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.isVariable));

            bool isVariable = isVariableProperty.boolValue;
            bool heightChanged = false;
            RenderActionInputInLine(
                variableDefinition,
                variableProperty,
                getVariableOptionsForType,
                getConstInputOptions,
                inputRect,
                true,
                variableDisplayName,
                true,
                GUIContent.none,
                ref actionInstanceRenderData.NeedsVerify,
                ref heightChanged,
                actionInstanceRenderData.AllowsUnityObjectConstants,
                actionInstanceRenderData.ControlHint);

            if (heightChanged ||
                isVariable != isVariableProperty.boolValue)
            {
                actionInstanceRenderData.NeedsRedraws = true;
                heightChanged = false;
            }

            actionInstanceRenderData.ContainsNull |= InputContainsNullVariableOrValue(variableProperty);
            
            if (layout)
            {
                EditorGUILayout.EndHorizontal();
            }

            // TODO handle other multiline editor types
            if (!actionInstanceRenderData.NeedsRedraws &&
                propertyType.IsArray && 
                !isVariableProperty.boolValue) // Or is a type that is multiline editor
            {
                SerializedProperty dataProperty =
                    variableProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.data));
                
                inputRect.y += EditorGUIUtility.singleLineHeight + 5;
                inputRect.height = rect.height - EditorGUIUtility.singleLineHeight;

                bool prevShow = actionInstanceRenderData.ExpandedInputs[inputIndex];

                int size = 0;
                // On first creation, this only can be null?
                // TODO figure out why
                if (actionInstanceRenderData.InputLists != null)
                {
                    size = actionInstanceRenderData.InputLists[inputIndex]?.count ?? 0;
                }
                
                GUIContent content =
                    new GUIContent(variableDefinition.displayName, variableDefinition.description);
                DrawArrayEditor(
                    dataProperty,
                    content,
                    propertyType,
                    ref actionInstanceRenderData.ExpandedInputs[inputIndex],
                    () => heightChanged = true,
                    ref actionInstanceRenderData.InputLists[inputIndex],
                    actionInstanceRenderData.ControlHint,
                    layout, 
                    inputRect);

                if (heightChanged 
                    || prevShow != actionInstanceRenderData.ExpandedInputs[inputIndex] 
                    || size != (actionInstanceRenderData.InputLists[inputIndex]?.count ?? 0))
                {
                    actionInstanceRenderData.NeedsRedraws = true;
                }
            }
        }

        private static void CreateActionVariableInstanceMultiInputEditor(
            CyanTriggerActionInstanceRenderData actionInstanceRenderData,
            int inputIndex,
            SerializedProperty variableProperty,
            CyanTriggerActionVariableDefinition variableDefinition,
            Func<Type, List<CyanTriggerEditorVariableOption>> getVariableOptionsForType)
        {
            if (actionInstanceRenderData.InputLists[inputIndex] != null)
            {
                return;
            }
            
            string ControlHintPerIndex(int index)
            {
                return $"{actionInstanceRenderData.ControlHint}-{index}";
            }
            
            Type propertyType = variableDefinition.type.Type;
            bool displayEditorInLine = TypeHasInLineEditor(propertyType);
            
            ReorderableList list = new ReorderableList(
                variableProperty.serializedObject, 
                variableProperty, 
                true, 
                false, 
                true, 
                true);
            list.headerHeight = 0;
            list.drawElementCallback = (elementRect, index, isActive, isFocused) =>
            {
                // Remove the 2 pixel padding
                elementRect.y += 1;
                elementRect.height -= 2;
                
                SerializedProperty property = variableProperty.GetArrayElementAtIndex(index);
                bool heightChanged = false;
                RenderActionInputInLine(
                    variableDefinition,
                    property,
                    getVariableOptionsForType,
                    null,
                    elementRect,
                    false,
                    GUIContent.none,
                    displayEditorInLine,
                    new GUIContent("Select to Edit"),
                    ref actionInstanceRenderData.NeedsVerify,
                    ref heightChanged,
                    actionInstanceRenderData.AllowsUnityObjectConstants,
                    ControlHintPerIndex(index));
                
                if (heightChanged)
                {
                    actionInstanceRenderData.NeedsRedraws = true;
                }
            };
            list.elementHeightCallback = index =>
            {
                SerializedProperty property = variableProperty.GetArrayElementAtIndex(index);
                SerializedProperty isVariableProperty = 
                    property.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.isVariable));
                bool isVariable = isVariableProperty.boolValue;
                float height = GetHeightForActionInputInLineEditor(variableDefinition, isVariable, list.list?[index], ControlHintPerIndex(index));

                // Unity 2022 adds a 2 pixel padding, which looks good. Add this back to 2019 version.
#if !UNITY_2022_3_OR_NEWER
                height += 2;
#endif
                
                return height;
            };
            list.onReorderCallback = reorderableList => actionInstanceRenderData.NeedsRedraws = true;

            actionInstanceRenderData.InputLists[inputIndex] = list;
        }
        
        public static void DrawActionVariableInstanceMultiInputEditor(
            CyanTriggerActionInstanceRenderData actionInstanceRenderData,
            int inputIndex,
            SerializedProperty variableProperty,
            CyanTriggerActionVariableDefinition variableDefinition,
            Func<Type, List<CyanTriggerEditorVariableOption>> getVariableOptionsForType,
            ref Rect rect,
            bool layout = false)
        {
            CreateActionVariableInstanceMultiInputEditor(
                actionInstanceRenderData, 
                inputIndex, 
                variableProperty,
                variableDefinition, 
                getVariableOptionsForType);

            if (layout)
            {
                EditorGUILayout.Space();
            }

            Type propertyType = variableDefinition.type.Type;
            bool displayEditorInLine = TypeHasInLineEditor(propertyType);
            
            rect.height =
                GetHeightForActionVariableInstanceMultiInputEditor(
                    propertyType,
                    actionInstanceRenderData.ExpandedInputs[inputIndex],
                    actionInstanceRenderData.InputLists[inputIndex]);
            Rect inputRect = new Rect(rect);
            inputRect.height = FoldoutListHeaderAreaHeight;

            GUIContent variableDisplayName =
                new GUIContent(variableDefinition.displayName, variableDefinition.description);

            bool prevExpand = actionInstanceRenderData.ExpandedInputs[inputIndex];
            int arraySize = variableProperty.arraySize;
            
            DrawFoldoutListHeader(
                variableDisplayName,
                ref actionInstanceRenderData.ExpandedInputs[inputIndex],
                true,
                variableProperty.arraySize,
                size =>
                {
                    int prevSize = variableProperty.arraySize;
                    variableProperty.arraySize = size;

                    for (int i = prevSize; i < size; ++i)
                    {
                        SerializedProperty property = variableProperty.GetArrayElementAtIndex(i);
                        SerializedProperty dataProperty =
                            property.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.data));
                        CyanTriggerSerializableObject.UpdateSerializedProperty(dataProperty, null);
                    }
                },
                // Only allow drag for GameObject or Component fields
                typeof(GameObject).IsAssignableFrom(propertyType) ||
                typeof(Component).IsAssignableFrom(propertyType) ||
                typeof(IUdonEventReceiver).IsAssignableFrom(propertyType),
                dragObjects =>
                {
                    List<Object> objects = GetGameObjectsOrComponentsFromDraggedObjects(dragObjects, propertyType);

                    int startIndex = variableProperty.arraySize;
                    variableProperty.arraySize += objects.Count;
                    for (int i = 0; i < objects.Count; ++i)
                    {
                        SerializedProperty property = variableProperty.GetArrayElementAtIndex(startIndex + i);
                        SerializedProperty isVarProperty =
                            property.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.isVariable));
                        isVarProperty.boolValue = false;
                        SerializedProperty dataProperty =
                            property.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.data));
                        
                        CyanTriggerSerializableObject.UpdateSerializedProperty(dataProperty, objects[i]);
                    }
                    variableProperty.serializedObject.ApplyModifiedProperties();
                },
                false,
                true,
                layout,
                inputRect);

            if (actionInstanceRenderData.ExpandedInputs[inputIndex])
            {
                if (layout)
                {
                    GUILayout.Space(2);
                    actionInstanceRenderData.InputLists[inputIndex].DoLayoutList();
                }
                else
                {
                    Rect listRect = new Rect(rect);
                    listRect.y += FoldoutListHeaderAreaHeight;
                    listRect.height -= FoldoutListHeaderAreaHeight;
                    actionInstanceRenderData.InputLists[inputIndex].DoList(listRect);
                }
            }
            
            if (prevExpand != actionInstanceRenderData.ExpandedInputs[inputIndex] ||
                arraySize != variableProperty.arraySize)
            {
                arraySize = variableProperty.arraySize;
                actionInstanceRenderData.NeedsRedraws = true;
            }

            actionInstanceRenderData.ContainsNull |= arraySize == 0;
            for (int curInput = 0; curInput < arraySize && !actionInstanceRenderData.ContainsNull; ++curInput)
            {
                var inputProp = variableProperty.GetArrayElementAtIndex(curInput);
                actionInstanceRenderData.ContainsNull |= InputContainsNullVariableOrValue(inputProp);
            }

            // TODO figure out how to get the list here.
            if (!displayEditorInLine && actionInstanceRenderData.InputLists[inputIndex].index != -1)
            {
                EditorGUILayout.LabelField($"Selected item {actionInstanceRenderData.InputLists[inputIndex].index}");
            }
        }

        private static float GetHeightForActionInputInLineEditor(
            CyanTriggerActionVariableDefinition variableDefinition,
            bool isVariable,
            object data,
            string controlHint)
        {
            if ((variableDefinition.variableType & CyanTriggerActionVariableTypeDefinition.Hidden) != 0)
            {
                return 0;
            }
            bool allowsCustomValues =
                (variableDefinition.variableType & CyanTriggerActionVariableTypeDefinition.Constant) != 0;
            bool allowsVariables =
                (variableDefinition.variableType & CyanTriggerActionVariableTypeDefinition.VariableInput) != 0;
            
            if (!allowsCustomValues && !allowsVariables)
            {
                return 0;
            }
            
            bool allowsOutput =
                (variableDefinition.variableType & CyanTriggerActionVariableTypeDefinition.VariableOutput) != 0;
            if (allowsOutput)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            var type = variableDefinition.type.Type;

            // Heights for input arrays will be calculated else where
            if (type.IsArray)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            if (isVariable)
            {
                return EditorGUIUtility.singleLineHeight;
            }
            
            return HeightForInLineEditor(type, data, controlHint, true);
        }
        
        
        private static void RenderActionInputInLine(
            CyanTriggerActionVariableDefinition variableDefinition,
            SerializedProperty variableProperty,
            Func<Type, List<CyanTriggerEditorVariableOption>> getVariableOptionsForType,
            Func<List<(GUIContent, object)>> getConstInputOptions,
            Rect rect,
            bool displayLabel,
            GUIContent labelContent,
            bool displayEditor,
            GUIContent editorLabelContent,
            ref bool needsVerify,
            ref bool heightChanged,
            bool allowsUnityObjectConstants,
            string controlHint)
        {
            SerializedProperty dataProperty =
                variableProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.data));

            // Skip hidden input, but set default value
            if ((variableDefinition.variableType & CyanTriggerActionVariableTypeDefinition.Hidden) != 0)
            {
                // TODO verify this works?
                CyanTriggerSerializableObject.UpdateSerializedProperty(dataProperty, variableDefinition.defaultValue?.Obj);
                return;
            }

            bool allowsCustomValues =
                (variableDefinition.variableType & CyanTriggerActionVariableTypeDefinition.Constant) != 0;
            bool allowsVariables =
                (variableDefinition.variableType & CyanTriggerActionVariableTypeDefinition.VariableInput) != 0;
            bool outputVar = 
                (variableDefinition.variableType & CyanTriggerActionVariableTypeDefinition.VariableOutput) != 0;
            
            Type propertyType = variableDefinition.type.Type;
            
            // If is AssetCyanTrigger and type is Unity Object,
            // do not allow direct constants as unity can't serialize it properly
            if (!allowsUnityObjectConstants && (typeof(Object).IsAssignableFrom(propertyType) 
                                                || typeof(IUdonEventReceiver).IsAssignableFrom(propertyType)))
            {
                allowsCustomValues = false;
            }

            // TODO verify this isn't possible. What
            if (!allowsCustomValues && !allowsVariables)
            {
                return;
            }

            SerializedProperty isVariableProperty =
                variableProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.isVariable));
            SerializedProperty idProperty =
                variableProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.variableID));
            SerializedProperty nameProperty =
                variableProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.name));

            bool isVariable = isVariableProperty.boolValue;


            float spaceBetween = 5;
            float width = (rect.width - spaceBetween * 2) / 3f;
            Rect labelRect = new Rect(rect.x, rect.y, width, Mathf.Min(rect.height, EditorGUIUtility.singleLineHeight));
            Rect inputRectFull = new Rect(labelRect.xMax + spaceBetween, rect.y, width * 2 + spaceBetween, rect.height);
            Rect typeRect = new Rect(labelRect.xMax + spaceBetween, rect.y, Mathf.Min(width * 0.5f, 65f), rect.height);
            Rect inputRect = new Rect(typeRect.xMax + spaceBetween, rect.y, width * 2 - typeRect.width, rect.height);

            // TODO verify variable value and show error if not valid
            //if (!CyanTriggerUtil.IsValidActionVariableInstance(variableProperty))
            
            // TODO do this properly
            // var valid = variableInstance.IsValid();
            // if (valid != CyanTriggerUtil.InvalidReason.Valid)
            // {
            //     labelRect = CyanTriggerPropertyEditor.DrawErrorIcon(labelRect, valid.ToString());
            // }
            
            if (displayLabel)
            {
                string propertyTypeFriendlyName = CyanTriggerNameHelpers.GetTypeFriendlyName(propertyType);
                if (string.IsNullOrEmpty(labelContent.text))
                {
                    labelContent.text = $"{(outputVar? "out " : "")}{propertyTypeFriendlyName}";
                }

                string updatedTooltip = $"{labelContent.text} ({propertyTypeFriendlyName})";
                if (outputVar)
                {
                    updatedTooltip = $"{updatedTooltip} - The contents of this variable will be modified.";
                }
                if (!string.IsNullOrEmpty(labelContent.tooltip))
                {
                    updatedTooltip += $"\n{labelContent.tooltip}";
                }
                labelContent.tooltip = updatedTooltip;
                
                // TODO show indicator if variable will be edited
                EditorGUI.LabelField(labelRect, labelContent);
            }
            else
            {
                inputRectFull.x -= labelRect.width;
                typeRect.x -= labelRect.width;
                inputRect.x -= labelRect.width;

                inputRectFull.width += labelRect.width;
                inputRect.width += labelRect.width;
            }

            Rect customRect = inputRectFull;
            if (allowsCustomValues && allowsVariables)
            {
                Rect popupRect = typeRect;
                if (!isVariable && propertyType.IsArray && displayEditor)
                {
                    popupRect = inputRectFull;
                }
                popupRect.height = EditorGUIUtility.singleLineHeight;

                string[] options = {"Input", "Variable"};
                EditorGUI.BeginProperty(popupRect, GUIContent.none, isVariableProperty);
                isVariable = isVariableProperty.boolValue = 1 == EditorGUI.Popup(popupRect, isVariable ? 1 : 0, options);
                EditorGUI.EndProperty();
                customRect = inputRect;
            }
            else if (allowsCustomValues)
            {
                isVariable = isVariableProperty.boolValue = false;
            }
            else
            {
                isVariable = isVariableProperty.boolValue = true;
            }
            
            if (isVariable)
            {
                int selected = 0;
                List<string> options = new List<string>();
                List<CyanTriggerEditorVariableOption> varOptions = getVariableOptionsForType(propertyType);
                List<CyanTriggerEditorVariableOption> visibleOptions = new List<CyanTriggerEditorVariableOption>();

                // Check if the variable type is output only.
                bool createNewVar = outputVar && !allowsCustomValues;
                options.Add(createNewVar ? "+New" : "None");

                string idValue = idProperty.stringValue;
                bool isEmpty = string.IsNullOrEmpty(idValue);
                string nameValue = nameProperty.stringValue;
                
                // Go through and add all variable options, checking for which is the current selected item.
                foreach (var varOption in varOptions)
                {
                    // Skip readonly variables for output var options
                    if (outputVar && varOption.IsReadOnly)
                    {
                        continue;
                    }
                    
                    if (idValue == varOption.ID || (isEmpty && nameValue == varOption.Name))
                    {
                        selected = options.Count;
                    }
                    visibleOptions.Add(varOption);

                    string optionName = propertyType != varOption.Type
                        ? $"{varOption.Name} ({CyanTriggerNameHelpers.GetTypeFriendlyName(varOption.Type)})"
                        : varOption.Name;
                    options.Add(optionName);
                }
                
                // TODO add option for new global variable or new local variable which creates the variable before this action
                // Is this needed if outputs are always new?

                
                // When displaying for out variables that do not allow inputs,
                // Add option for new variable and a space for the variable name.
                if (createNewVar && selected == 0)
                {
                    customRect = typeRect;
                    bool dirty = DrawEditor(dataProperty, inputRect, GUIContent.none, typeof(string), ref heightChanged, controlHint);

                    if (dirty)
                    {
                        // Sanitize names to prevent weird characters. Note that this is just for display as the actual
                        // variable name will be generated at compile time.
                        string varName = (string)CyanTriggerSerializableObject.ObjectFromSerializedProperty(dataProperty);
                        string sanitizedName = CyanTriggerNameHelpers.SanitizeName(varName);
                        if (!string.IsNullOrEmpty(varName) && varName != sanitizedName)
                        {
                            CyanTriggerSerializableObject.UpdateSerializedProperty(dataProperty, sanitizedName);
                        }
                    }
                    
                    
                    // TODO verify unique names for variable providers. (Is this even the right place for that?)
                }

                int prevSelected = selected;
                EditorGUI.BeginProperty(customRect, GUIContent.none, idProperty);
                EditorGUI.BeginProperty(customRect, GUIContent.none, nameProperty);
                selected = EditorGUI.Popup(customRect, selected, options.ToArray());
                EditorGUI.EndProperty();
                EditorGUI.EndProperty();

                // Swapping between new variable and existing variables should cause a ui redraw to recreate lists and verify data.
                if (createNewVar && selected != prevSelected && (prevSelected == 0 || selected == 0))
                {
                    needsVerify = true;
                }
                
                if (selected == 0)
                {
                    nameProperty.stringValue = "";
                    
                    if (createNewVar)
                    {
                        // TODO move this to better location?
                        if (prevSelected != 0 || string.IsNullOrEmpty(idProperty.stringValue))
                        {
                            idProperty.stringValue = Guid.NewGuid().ToString();
                        }
                    }
                    else
                    {
                        idProperty.stringValue = "";
                    }
                }
                else
                {
                    var varOption = visibleOptions[selected - 1];
                    idProperty.stringValue = varOption.ID;
                    nameProperty.stringValue = varOption.Name;
                }
            }
            else if (!displayEditor)
            {
                EditorGUI.LabelField(customRect, editorLabelContent);
            }
            else if (!propertyType.IsArray)
            {
                if (customRect.height > EditorGUIUtility.singleLineHeight)
                {
                    customRect = new Rect(rect);
                    customRect.yMin += EditorGUIUtility.singleLineHeight;
                }
                
                // TODO verify unique names for variable providers. (Is this even the right place for that?)
                // Note that variable providers are obsolete and most likely shouldn't be addressed here as this is for general non variable properties
                DrawEditor(dataProperty, customRect, GUIContent.none, propertyType, ref heightChanged, controlHint, false, getConstInputOptions);
            }
            else
            {
                // Cannot edit arrays here, please call RenderActionInputArray directly
            }
        }

        public static bool InputContainsNullVariableOrValue(SerializedProperty variableProperty)
        {
            SerializedProperty isVariableProperty =
                variableProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.isVariable));

            if (isVariableProperty.boolValue)
            {
                SerializedProperty idProperty =
                    variableProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.variableID));
                SerializedProperty nameProperty =
                    variableProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.name));

                return string.IsNullOrEmpty(idProperty.stringValue) && string.IsNullOrEmpty(nameProperty.stringValue);
            }
            
            SerializedProperty dataProperty =
                variableProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.data));
            return CyanTriggerSerializableObject.ObjectFromSerializedProperty(dataProperty) == null;
        }
    }
}