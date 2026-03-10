using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEditor;
using UnityEngine;

namespace Cyan.CT.Editor
{
    // Note that this Dynamic property editor system does not work for properties that have delayed value setting.
    // Example being LayerMask in unity 2022. 
    // TODO Support delayed property setting by creating a unique property editor for each "controlHint".
    // This may have bad performance, but will work in all cases. :upsidedown:
    // All the hacks for saving expanded children can be removed though, so it may not actually be that bad on performance.
    public sealed class CyanTriggerTypePropertyEditorDynamic : CyanTriggerTypePropertyEditor
    {
        private static readonly object ScriptAttributeUtility;
        private static readonly MethodInfo GetDrawerTypeForType;

        static CyanTriggerTypePropertyEditorDynamic()
        {
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            ScriptAttributeUtility = assembly.CreateInstance("UnityEditor.ScriptAttributeUtility");
            var scriptAttributeUtilityType = ScriptAttributeUtility.GetType();
            
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;
            GetDrawerTypeForType = scriptAttributeUtilityType.GetMethod("GetDrawerTypeForType", bindingFlags);
        }
        
        private static bool HasPropertyDrawer(Type classType)
        {
            return GetDrawerTypeForType.Invoke(ScriptAttributeUtility, new object[] { classType }) != null;
        }
        
        private readonly ScriptableObject _obj;
        private readonly SerializedObject _serObj; 
        private readonly FieldInfo _valueField;
        private readonly SerializedProperty _prop;

        // Stores for each Id the expand value for every property in that id.
        private readonly Dictionary<string, Dictionary<string, bool>> _expandedValues = new Dictionary<string, Dictionary<string, bool>>();

        private readonly bool _isSingleLine;
        private readonly bool _shouldShowExpandLabel;
        
        public CyanTriggerTypePropertyEditorDynamic(Type type) : base(type, 0)
        {
            _obj = CreateNewObject(type);
            _serObj = new SerializedObject(_obj);
            _prop = _serObj.FindProperty("value");
            _valueField = _obj.GetType().GetField("value");

            // Property does not have serializable field and cannot have an editor. Return early to prevent NREs
            if (!HasEditor())
            {
                _serObj.Dispose();
                return;
            }
            
            _prop.isExpanded = true;

            // If something has children items, assume it will need extra space and cannot be rendered on a single line.
            bool singleLineHeight = GetPropertyHeightDirect(true) <= EditorGUIUtility.singleLineHeight;
            _isSingleLine = !_prop.hasVisibleChildren && singleLineHeight;
            
            // If something has a custom editor, do not ever show the Expand Label
            bool hasCustomEditor = HasPropertyDrawer(Type);
            _shouldShowExpandLabel = !hasCustomEditor && !singleLineHeight;
        }

        public bool IsValid()
        {
            return _obj != null;
        }
        
        public void ClearExpandValues()
        {
            _expandedValues.Clear();
        }

        public override bool HasEditor()
        {
            return _prop != null;
        }

        public override bool IsSingleLine()
        {
            return _isSingleLine;
        }
        
        // Should show a label for the property, giving area to click for expanding or collapsing the field.
        private bool HasExpand()
        {
            return _shouldShowExpandLabel;
        }

        public override float GetPropertyHeight(object value, string controlHint)
        {
            SetValue(value);
            SetChildrenExpand(controlHint);
            
            // Hacky situation to force the rest of the editors to draw this on multiline even when height is exactly one line.
            float height = 0;
            bool expand = HasExpand();
            if (!IsSingleLine() || expand)
            {
                height = 1;
            }
            height += GetPropertyHeightDirect(!expand);

            return height;
        }
        
        private float GetPropertyHeightDirect(bool emptyContent = false)
        {
            return emptyContent 
                ? EditorGUI.GetPropertyHeight(_prop, GUIContent.none, true)
                : EditorGUI.GetPropertyHeight(_prop, true);
        }

        public override object DrawProperty(Rect rect, object value, GUIContent content, bool layout, ref bool heightChanged, string controlHint)
        {
            // Prevent Errors when the object has been disposed
            if (!IsValid())
            {
                return value;
            }
            
            SetValue(value);
            SetChildrenExpand(controlHint);

            EditorGUI.BeginChangeCheck();
            
            if (layout)
            {
                EditorGUILayout.PropertyField(_prop, content, true);
            }
            else
            {
                var width = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Mathf.Min(width, rect.width / 3);
                
                content = GUIContent.none;
                if (HasExpand())
                {
                    rect.xMin += 8;
                    string label = $"{CyanTriggerNameHelpers.GetTypeFriendlyName(Type)} Value";
                    content = new GUIContent(label, label);
                }
                
                EditorGUI.PropertyField(rect, _prop, content, true);
                
                EditorGUIUtility.labelWidth = width;
            }

            if (EditorGUI.EndChangeCheck())
            {
                heightChanged = true;
                GetChildrenExpand(controlHint);
                return GetValue();
            }

            return value;
        }
        
        private void SetValue(object t)
        {
            _valueField.SetValue(_obj, t);
            _serObj.Update();
        }
        
        private object GetValue()
        {
            _serObj.ApplyModifiedProperties();
            return _valueField.GetValue(_obj);
        }

        private void SetChildrenExpand(string id)
        {
            if (!_expandedValues.TryGetValue(id, out var expandMap))
            {
                expandMap = new Dictionary<string, bool>();
                _expandedValues.Add(id, expandMap);
                expandMap.Add(_prop.propertyPath, true);
            }

            var itr = _prop.Copy();
            do
            {
                if (!expandMap.TryGetValue(itr.propertyPath, out bool expand))
                {
                    expand = false;
                }

                itr.isExpanded = expand;
            } while (itr.Next(true));
        }

        private void GetChildrenExpand(string id)
        {
            if (!_expandedValues.TryGetValue(id, out var expandMap))
            {
                expandMap = new Dictionary<string, bool>();
                _expandedValues.Add(id, expandMap);
            }
            expandMap.Clear();

            var itr = _prop.Copy();
            do
            {
                if (itr.hasChildren)
                {
                    expandMap.Add(itr.propertyPath, itr.isExpanded);
                }
            } while (itr.Next(true));
        }


        // Class generation taken from stackoverflow: https://stackoverflow.com/a/3862241
        // Create a new scriptable object type that has one public variable of the given type, allowing use of Unity's
        // SerializedObject and SerializedProperty options for drawing an inspector just for that type.
        #region Class Generation
        
        private static ScriptableObject CreateNewObject(Type type)
        {
            var myType = CompileResultType(type);
            return ScriptableObject.CreateInstance(myType);
        }
        
        private static Type CompileResultType(Type type)
        {
            TypeBuilder tb = GetTypeBuilder(type);
            tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            CreateProperty(tb, "value", type);

            Type objectType = tb.CreateType();
            return objectType;
        }

        private static TypeBuilder GetTypeBuilder(Type t)
        {
            var typeSignature = $"CyanTriggerDynamicType_{CyanTriggerNameHelpers.GetSanitizedTypeName(t)}";
            var an = new AssemblyName(typeSignature);
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder tb = moduleBuilder.DefineType(typeSignature,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    typeof(ScriptableObject)); // Ensure that Unity can serialize the object.
            return tb;
        }

        private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder = tb.DefineField(propertyName, propertyType, FieldAttributes.Public);

            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);
        }

        #endregion
    }
}