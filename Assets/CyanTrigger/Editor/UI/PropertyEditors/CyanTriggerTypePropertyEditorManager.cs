using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace Cyan.CT.Editor
{
    public static class CyanTriggerTypePropertyEditorManager
    {
        private static readonly HashSet<Type> InvalidTypeEditors = new HashSet<Type>();
        private static readonly HashSet<Type> CustomTypes = new HashSet<Type>();
        private static readonly Dictionary<Type, CyanTriggerTypePropertyEditor> CachedDynamicTypes = 
            new Dictionary<Type, CyanTriggerTypePropertyEditor>();

        private static readonly Dictionary<Type, Type> TypeConversion = new Dictionary<Type, Type>()
        {
            {typeof(IUdonEventReceiver), typeof(UdonBehaviour)}
        };

        static CyanTriggerTypePropertyEditorManager()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            
            // Specialized inspectors over unity default inspector
            AddPropertyType(new CyanTriggerTypePropertyEditorBool());
            AddPropertyType(new CyanTriggerTypePropertyEditorMatrix4x4());
            AddPropertyType(new CyanTriggerTypePropertyEditorQuaternion());
            AddPropertyType(new CyanTriggerTypePropertyEditorVector4());
            AddPropertyType(new CyanTriggerTypePropertyEditorVRCPlayerApi());
            AddPropertyType(new CyanTriggerTypePropertyEditorType());
            
            // Dynamic breaks change checks in 2022
            AddPropertyType(new CyanTriggerTypePropertyEditorLayerMask());
            
            // Non serializable types
            AddPropertyType(new CyanTriggerTypePropertyEditorPlane());
            AddPropertyType(new CyanTriggerTypePropertyEditorRay());
        }

        // Ensure on playmode change that serialized objects are properly removed rather than error out.
        private static void PlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            ClearCustomTypes();
        }
        
        // Ensure on scene open that serialized objects are properly removed rather than error out.
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ClearCustomTypes();
        }

        private static void ClearCustomTypes()
        {
            foreach (var type in CustomTypes)
            {
                CachedDynamicTypes.Remove(type);
            }
            CustomTypes.Clear();
        }

        public static void ClearDynamicTypeExpandValues()
        {
            foreach (var type in CustomTypes)
            {
                if (CachedDynamicTypes.TryGetValue(type, out var editor) && editor is CyanTriggerTypePropertyEditorDynamic dynamicEditor)
                {
                    dynamicEditor.ClearExpandValues();
                }
            }
        }

        private static bool TryGetCachedEditor(Type type, out CyanTriggerTypePropertyEditor editor)
        {
            if (CachedDynamicTypes.TryGetValue(type, out editor))
            {
                // Clear invalid dynamic editors to allow for creating new versions.
                // This happens on playmode switch and scene loading.
                // The overall system is hacky, so this is just another hack on top of it all :upsidedown:
                if (editor is CyanTriggerTypePropertyEditorDynamic dynamicEditor && !dynamicEditor.IsValid())
                {
                    CachedDynamicTypes.Remove(type);
                    CustomTypes.Remove(type);
                    return false;
                }
                
                return true;
            }

            return false;
        }

        private static CyanTriggerTypePropertyEditor AddPropertyType(CyanTriggerTypePropertyEditor propertyEditor)
        {
            Type type = propertyEditor.GetPropertyType();
            if (propertyEditor.HasEditor())
            {
                // Save type for dynamic inspector to help with garbage collection on enter/exit playmode.
                if (propertyEditor is CyanTriggerTypePropertyEditorDynamic)
                {
                    CustomTypes.Add(type);
                }
                
                CachedDynamicTypes[type] = propertyEditor;
                return propertyEditor;
            }
            
            InvalidTypeEditors.Add(type);
            return CyanTriggerTypePropertyEditor.InvalidEditor;
        }
        
        public static CyanTriggerTypePropertyEditor GetPropertyEditor(Type type)
        {
            if (TypeConversion.TryGetValue(type, out Type convertedType))
            {
                type = convertedType;
            }
            
            if (InvalidTypeEditors.Contains(type))
            {
                return CyanTriggerTypePropertyEditor.InvalidEditor;
            }
            
            if (TryGetCachedEditor(type, out var propertyEditor))
            {
                return propertyEditor;
            }

            return AddPropertyType(new CyanTriggerTypePropertyEditorDynamic(type));
        }
    }
}