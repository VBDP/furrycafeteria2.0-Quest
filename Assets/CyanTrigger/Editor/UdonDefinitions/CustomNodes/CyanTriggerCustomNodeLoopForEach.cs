using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.Udon.Graph;

namespace Cyan.CT.Editor
{
    public class CyanTriggerCustomNodeLoopForEach :
        CyanTriggerCustomUdonActionNodeDefinition,
        ICyanTriggerCustomNodeLoop,
        ICyanTriggerCustomNodeScope,
        ICyanTriggerCustomNodeDependency,
        ICyanTriggerCustomNodeCustomVariableInitialization
    {
        public const string FullName = "CyanTriggerSpecial_ForEach";
        private static readonly Type CollectionType = typeof(IEnumerable);

        private static Type _dataListType;
        private static Type _dataDictionaryType;

        // This is risky, but using it to prevent compile errors by referencing the DataDictionary and DataList types directly.
        // TODO remove once DataDictionary and DataLists are more widely used.
        private static Type DataListType {
            get
            {
                if (_dataListType == null)
                {
                    var def = CyanTriggerNodeDefinitionManager.Instance.GetDefinition("Type_VRCSDK3DataDataList");
                    if (def != null)
                    {
                        _dataListType = def.BaseType;
                    }
                }
                return _dataListType;
            }
        }
        
        private static Type DataDictionaryType {
            get
            {
                if (_dataDictionaryType == null)
                {
                    var def = CyanTriggerNodeDefinitionManager.Instance.GetDefinition("Type_VRCSDK3DataDataDictionary");
                    if (def != null)
                    {
                        _dataDictionaryType = def.BaseType;
                    }
                }
                return _dataDictionaryType;
            }
        }
        
        

        public static readonly UdonNodeDefinition NodeDefinition = new UdonNodeDefinition(
            "ForEach",
            FullName,
            typeof(CyanTrigger),
            new []
            {
                new UdonNodeParameter
                {
                    name = "Collection",
                    type = CollectionType,
                    parameterType = UdonNodeParameter.ParameterType.IN
                },
                new UdonNodeParameter
                {
                    name = "index",
                    type = typeof(int),
                    parameterType = UdonNodeParameter.ParameterType.OUT
                },
                new UdonNodeParameter
                {
                    name = "value",
                    type = typeof(object),
                    parameterType = UdonNodeParameter.ParameterType.OUT
                }
            },
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<object>(),
            true
        );
        
        public override UdonNodeDefinition GetNodeDefinition()
        {
            return NodeDefinition;
        }

        public override CyanTriggerNodeDefinition.UdonDefinitionType GetDefinitionType()
        {
            return CyanTriggerNodeDefinition.UdonDefinitionType.CyanTriggerSpecial;
        }

        public override string GetDisplayName()
        {
            return NodeDefinition.name;
        }
        
        public override string GetDocumentationLink()
        {
            return CyanTriggerDocumentationLinks.ForeachNodeDocumentation;
        }

        public override void AddActionToProgram(CyanTriggerCompileState compileState)
        {
            var actionInstance = compileState.ActionInstance;
            var program = compileState.Program;
            var data = program.Data;
            var actions = compileState.ActionMethod;
            
            var scopeFrame = compileState.ScopeData.ScopeStack.Peek();
            scopeFrame.EndNop = CyanTriggerAssemblyInstruction.Nop();
            scopeFrame.StartNop = CyanTriggerAssemblyInstruction.Nop();
            
            Type intType = typeof(int);
            
            var startInput = data.GetOrCreateVariableConstant(intType, 0);
            var stepInput = data.GetOrCreateVariableConstant(intType, 1);

            var collectionInput =
                compileState.GetDataFromVariableInstance(-1, 0, actionInstance.inputs[0], CollectionType, false);
            var collectionType = collectionInput.Type;
            
            if (collectionType == CollectionType)
            {
                compileState.LogError("ForEach must have a collection to iterate over.");
                return;
            }
            
            Type dataListType = DataListType;
            Type dataDictionaryType = DataDictionaryType;
            
            // TODO update this as more type support has been implemented.
            if (collectionType != null
                && !collectionType.IsArray
                && collectionType != typeof(string) 
                && collectionType != typeof(Transform) 
                && collectionType != typeof(RectTransform) 
                && collectionType != dataListType
                && collectionType != dataDictionaryType)
            {
                compileState.LogError($"ForEach does not currently support {collectionType.FullName}. Please create a bug report to have this implemented.");
                return;
            }
            
            bool isDataList = collectionType == dataListType;
            // Convert DataDictionary to DataList of keys.
            if (collectionType == dataDictionaryType)
            {
                isDataList = true;
                var tempDataList = data.RequestTempVariable(dataListType); // Purposefully do not release temp var
                actions.AddAction(CyanTriggerAssemblyInstruction.PushVariable(collectionInput));
                actions.AddAction(CyanTriggerAssemblyInstruction.PushVariable(tempDataList));
                actions.AddAction(CyanTriggerAssemblyInstruction.CreateExtern(
                    CyanTriggerDefinitionResolver.GetMethodSignature(
                        collectionType.GetMethod("GetKeys"))));

                collectionType = dataListType;
                collectionInput = tempDataList;
            }
            
            // Convert string to char[]
            else if (collectionType == typeof(string))
            {
                collectionType = typeof(char[]);
                var tempCharArray = data.RequestTempVariable(collectionType); // Purposefully do not release temp var

                actions.AddAction(CyanTriggerAssemblyInstruction.PushVariable(collectionInput));
                actions.AddAction(CyanTriggerAssemblyInstruction.PushVariable(tempCharArray));
                actions.AddAction(CyanTriggerAssemblyInstruction.CreateExtern(
                    CyanTriggerDefinitionResolver.GetMethodSignature(
                        typeof(string).GetMethod(nameof(string.ToCharArray), Array.Empty<Type>()))));

                collectionInput = tempCharArray;
            }

            var endInput = data.RequestTempVariable(intType); // Purposefully do not release temp var
            
            var indexVariable = compileState.GetDataFromVariableInstance(-1, 1, actionInstance.inputs[1], intType, false);
            var objVariable = compileState.GetDataFromVariableInstance(-1, 2, actionInstance.inputs[2], typeof(object), false);

            actions.AddAction(CyanTriggerAssemblyInstruction.PushVariable(collectionInput));
            actions.AddAction(CyanTriggerAssemblyInstruction.PushVariable(endInput));

            if (collectionType.IsArray)
            {
                actions.AddAction(CyanTriggerAssemblyInstruction.CreateExtern(
                    CyanTriggerDefinitionResolver.GetMethodSignature(
                        // ReSharper disable once PossibleNullReferenceException
                        typeof(Array).GetProperty(nameof(Array.Length)).GetGetMethod())));
            }
            else if (collectionType == typeof(Transform) || collectionType == typeof(RectTransform))
            {
                actions.AddAction(CyanTriggerAssemblyInstruction.CreateExtern(
                    CyanTriggerDefinitionResolver.GetMethodSignature(
                        // ReSharper disable once PossibleNullReferenceException
                        typeof(Transform).GetProperty(nameof(Transform.childCount)).GetGetMethod())));
            }
            else if (isDataList)
            {
                actions.AddAction(CyanTriggerAssemblyInstruction.CreateExtern(
                    CyanTriggerDefinitionResolver.GetMethodSignature(
                        // ReSharper disable once PossibleNullReferenceException
                        collectionType.GetProperty("Count").GetGetMethod())));
            }

            List<CyanTriggerAssemblyInstruction> UpdateObjectVariable()
            {
                List<CyanTriggerAssemblyInstruction> getVarActions = new List<CyanTriggerAssemblyInstruction>();
                
                getVarActions.Add(CyanTriggerAssemblyInstruction.PushVariable(collectionInput));
                getVarActions.Add(CyanTriggerAssemblyInstruction.PushVariable(indexVariable));
                getVarActions.Add(CyanTriggerAssemblyInstruction.PushVariable(objVariable));

                if (collectionType.IsArray)
                {
                    getVarActions.Add(CyanTriggerAssemblyInstruction.CreateExtern(
                        CyanTriggerDefinitionResolver.GetMethodSignature(
                            typeof(Array).GetMethod(nameof(Array.GetValue), new [] {typeof (int)}))));
                }
                else if (collectionType == typeof(Transform) || collectionType == typeof(RectTransform))
                {
                    getVarActions.Add(CyanTriggerAssemblyInstruction.CreateExtern(
                        CyanTriggerDefinitionResolver.GetMethodSignature(
                            typeof(Transform).GetMethod(nameof(Transform.GetChild), new [] {typeof (int)}))));
                }
                else if (isDataList)
                {
                    getVarActions.Add(CyanTriggerAssemblyInstruction.CreateExtern(
                        CyanTriggerDefinitionResolver.GetMethodSignature(
                            // ReSharper disable once PossibleNullReferenceException
                            collectionType.GetProperty("Item").GetGetMethod())));
                }

                // Check if object's value changes.
                var changedVariables = new List<CyanTriggerAssemblyDataType> { objVariable };
                getVarActions.AddRange(compileState.GetVariableChangedActions(changedVariables));
                
                return getVarActions;
            }
            
            actions.AddActions(CyanTriggerCustomNodeLoopFor.BeginForLoop(
                program, 
                startInput, 
                endInput, 
                stepInput, 
                indexVariable, 
                scopeFrame.StartNop, 
                scopeFrame.EndNop, 
                compileState.GetVariableChangedActions,
                UpdateObjectVariable));
        }
        
        public void HandleEndScope(CyanTriggerCompileState compileState)
        {
            var actionMethod = compileState.ActionMethod;
            var scopeFrame = compileState.ScopeData.ScopeStack.Peek();
            actionMethod.AddActions(CyanTriggerCustomNodeLoopFor.EndForLoop(scopeFrame.StartNop, scopeFrame.EndNop));
        }

        public UdonNodeDefinition[] GetDependentNodes()
        {
            return new[]
            {
                CyanTriggerCustomNodeBlockEnd.NodeDefinition
            };
        }
        
        public void InitializeVariableProperties(
            SerializedProperty inputsProperty, 
            SerializedProperty multiInputsProperty)
        {
            // Array input left uninitialized but as Variable, since you cannot have input here
            {
                SerializedProperty inputProperty = inputsProperty.GetArrayElementAtIndex(0);
                SerializedProperty isVariableProperty =
                    inputProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.isVariable));
                isVariableProperty.boolValue = true;
            }
            
            // index variable initialized with name
            {
                SerializedProperty inputProperty = inputsProperty.GetArrayElementAtIndex(1);
                SerializedProperty nameDataProperty =
                    inputProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.data));
                CyanTriggerSerializableObject.UpdateSerializedProperty(nameDataProperty, "index_int");
                
                SerializedProperty idProperty =
                    inputProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.variableID));
                idProperty.stringValue = Guid.NewGuid().ToString();
                
                SerializedProperty isVariableProperty =
                    inputProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.isVariable));
                isVariableProperty.boolValue = true;
            }
            
            // value object variable initialized with name
            {
                SerializedProperty inputProperty = inputsProperty.GetArrayElementAtIndex(2);
                SerializedProperty nameDataProperty =
                    inputProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.data));
                CyanTriggerSerializableObject.UpdateSerializedProperty(nameDataProperty, "value_object");
                
                SerializedProperty idProperty =
                    inputProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.variableID));
                idProperty.stringValue = Guid.NewGuid().ToString();
                
                SerializedProperty isVariableProperty =
                    inputProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.isVariable));
                isVariableProperty.boolValue = true;
            }

            inputsProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}