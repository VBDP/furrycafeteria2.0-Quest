using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Cyan.CT.Editor
{
    public class CyanTriggerCustomNodeInspectorAnimatorGetParameter : ICyanTriggerCustomNodeInspector
    {
        private readonly Type _type;
        
        public CyanTriggerCustomNodeInspectorAnimatorGetParameter(Type type)
        {
            _type = type;
        }
        
        public string GetNodeDefinitionName()
        {
            if (_type == typeof(bool))
            {
                return "UnityEngineAnimator.__GetBool__SystemString__SystemBoolean";
            }

            if (_type == typeof(int))
            {
                return "UnityEngineAnimator.__GetInteger__SystemString__SystemInt32";
            }

            if (_type == typeof(float))
            {
                return "UnityEngineAnimator.__GetFloat__SystemString__SystemSingle";
            }
            
            return "";
        }

        public string GetCustomActionGuid()
        {
            return "";
        }

        public bool HasCustomHeight(CyanTriggerActionInstanceRenderData actionInstanceRenderData)
        {
            return false;
        }

        public float GetHeightForInspector(CyanTriggerActionInstanceRenderData actionInstanceRenderData)
        {
            throw new NotImplementedException();
        }
        
        public void RenderInspector(
            CyanTriggerActionInstanceRenderData actionInstanceRenderData,
            CyanTriggerActionVariableDefinition[] variableDefinitions, 
            Func<Type, List<CyanTriggerEditorVariableOption>> getVariableOptionsForType, 
            Rect rect, 
            bool layout)
        {
            var actionProperty = actionInstanceRenderData.Property;
            var inputListProperty = actionProperty.FindPropertyRelative(nameof(CyanTriggerActionInstance.inputs));
            
            var multiVarDef = variableDefinitions[0];
            var animatorInputProperty = inputListProperty.GetArrayElementAtIndex(0);
            
            Debug.Assert((multiVarDef.variableType & CyanTriggerActionVariableTypeDefinition.AllowsMultiple) == 0,
                "Animator Inspector does not properly support multiple variables!");
            
            Rect inputRect = new Rect(rect);
            
            // Render Animator single-input editor
            int inputIndex = 0;
            CyanTriggerPropertyEditor.DrawActionVariableInstanceInputEditor(
                actionInstanceRenderData,
                inputIndex,
                animatorInputProperty, 
                variableDefinitions[inputIndex],
                getVariableOptionsForType, 
                ref inputRect,
                layout,
                null);
            
            rect.y += inputRect.height + 5;
            rect.height -= inputRect.height + 5;
            
            
            Func<List<(GUIContent, object)>> getOptionsInput = null;
            SerializedProperty parameterNameInputProperty = inputListProperty.GetArrayElementAtIndex(1);
            SerializedProperty parameterIsVariableProperty =
                parameterNameInputProperty.FindPropertyRelative(nameof(CyanTriggerActionVariableInstance.isVariable));

            // Go through all Animator input in multi-input to get a list of parameter options to pick from.
            if (!parameterIsVariableProperty.boolValue)
            {
                var animator = CyanTriggerCustomNodeInspectorUtil.GetTypeFromInput<Animator>(
                    animatorInputProperty,
                    actionInstanceRenderData.DataInstance.variables,
                    actionInstanceRenderData.UdonBehaviour,
                    out bool containsSelf);

                // This shouldn't happen since there is no "this animator" parameter.
                // if (containsSelf && actionInstanceRenderData.UdonBehaviour != null)
                // {
                //     animators.Add(actionInstanceRenderData.UdonBehaviour.GetComponent<Animator>());
                // }

                List<AnimatorControllerParameter> parameters = null;
                if (animator != null)
                {
                    parameters = CyanTriggerCustomNodeInspectorUtil.GetAnimatorParameterOptions(
                        new List<Animator>(){animator}, _type);    
                }
                
                if (parameters != null && parameters.Count > 0)
                {
                    List<string> optionsSorted = new List<string>();
                    foreach (var parameter in parameters)
                    {
                        optionsSorted.Add(parameter.name);
                    }
                    optionsSorted.Sort();
                    var optionContent = new List<(GUIContent, object)>();
                    foreach (var variable in optionsSorted)
                    {
                        optionContent.Add((new GUIContent(variable), variable));
                    }
                    getOptionsInput = () => optionContent;
                }
            }

            inputRect = new Rect(rect);
            
            // Render parameter options input editor.
            inputIndex = 1;
            CyanTriggerPropertyEditor.DrawActionVariableInstanceInputEditor(
                actionInstanceRenderData,
                inputIndex,
                parameterNameInputProperty, 
                variableDefinitions[inputIndex],
                getVariableOptionsForType, 
                ref inputRect,
                layout,
                getOptionsInput);
            
            
            rect.y += inputRect.height + 5;
            rect.height -= inputRect.height + 5;
            inputRect = new Rect(rect);

            inputIndex = 2;
            CyanTriggerPropertyEditor.DrawActionVariableInstanceInputEditor(
                actionInstanceRenderData,
                inputIndex,
                inputListProperty.GetArrayElementAtIndex(inputIndex), 
                variableDefinitions[inputIndex],
                getVariableOptionsForType, 
                ref inputRect,
                layout,
                null);
        }
    }
}