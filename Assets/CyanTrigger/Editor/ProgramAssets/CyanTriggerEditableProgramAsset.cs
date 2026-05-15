using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.Udon;
using VRC.Udon.Editor.ProgramSources.Attributes;

[assembly: UdonProgramSourceNewMenu(typeof(Cyan.CT.Editor.CyanTriggerEditableProgramAsset), "CyanTrigger Program Asset")]

namespace Cyan.CT.Editor
{
    [CreateAssetMenu(menuName = "CyanTrigger/CyanTrigger Program Asset", fileName = "New CyanTrigger Program Asset", order = 6)]
    [HelpURL(CyanTriggerDocumentationLinks.EditableProgramAsset)]
    public class CyanTriggerEditableProgramAsset : CyanTriggerProgramAsset
    {
        // Methods mainly for CyanTriggerAsset's inspector
        public bool allowEditingInInspector;
        public bool expandInInspector;

#if !CYAN_TRIGGER_DEBUG
        [HideInInspector]
#endif
        public bool isLocked;
        
        protected override void DrawProgramSourceGUI(UdonBehaviour udonBehaviour, ref bool dirty)
        {
            ApplyUdonDataProperties(ctDataInstance, udonBehaviour, ref dirty);

            var ctAsset = GetMatchingCyanTriggerAsset(udonBehaviour);

            // Only show CTAsset field if this is for a GameObject rather than viewing the program directly.
            if (udonBehaviour != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("CyanTriggerAsset", ctAsset, typeof(CyanTriggerAsset), true);
                EditorGUI.EndDisabledGroup();
            }
            
            if (ctAsset != null)
            {
                dirty |= VerifyAssetVariables(new [] {ctAsset});
                ApplyToUdon(ctAsset, udonBehaviour, ref dirty);
            }
            else if (udonBehaviour != null)
            {
                // Draw info box and button to add CyanTriggerAsset
                EditorGUILayout.HelpBox("Add CyanTriggerAsset for better inspector", MessageType.Warning);
                if (GUILayout.Button("Add CyanTriggerAsset"))
                {
                    CyanTriggerAsset.AddFromUdonBehaviour(udonBehaviour);
                    return;
                }
                DrawPublicVariables(udonBehaviour, ref dirty);
            }
            
            ShowGenericInspectorGUI(udonBehaviour, ref dirty, true);
            
            ShowDebugInformation(udonBehaviour, ref dirty);
        }

        private static CyanTriggerAsset GetMatchingCyanTriggerAsset(UdonBehaviour udonBehaviour)
        {
            if (udonBehaviour == null)
            {
                return null;
            }

            foreach (var ctAsset in udonBehaviour.GetComponents<CyanTriggerAsset>())
            {
                if (ctAsset.assetInstance?.udonBehaviour == udonBehaviour)
                {
                    return ctAsset;
                }
            }

            return null;
        }

        public void ApplyToUdon(CyanTriggerAsset ctAsset, UdonBehaviour udonBehaviour, ref bool dirty)
        {
            if (HasUncompiledChanges)
            {
                return;
            }

            var assetInstance = ctAsset.assetInstance;
            dirty |= VerifyAssetVariableValuesMatchType(ctAsset, ctDataInstance?.variables);
            UpdatePublicVariables(ctDataInstance, udonBehaviour, ref dirty, assetInstance.variableData);
            
            if (!Mathf.Approximately(assetInstance.proximity, udonBehaviour.proximity))
            {
                udonBehaviour.proximity = assetInstance.proximity;
                dirty = true;
            }

            if (assetInstance.interactText != udonBehaviour.interactText)
            {
                udonBehaviour.interactText = assetInstance.interactText;
                dirty = true;
            }
        }
        
        public bool ApplyToUdon(CyanTriggerAsset ctAsset)
        {
            if (HasUncompiledChanges)
            {
                return false;
            }

            var assetInstance = ctAsset.assetInstance;
            UdonBehaviour udonBehaviour = assetInstance?.udonBehaviour;
            if (udonBehaviour == null)
            {
                return false;
            }
            
            bool dirty = false;
            ApplyUdonDataProperties(ctDataInstance, udonBehaviour, ref dirty);
            ApplyToUdon(ctAsset, udonBehaviour, ref dirty);

            if (dirty)
            {
                EditorUtility.SetDirty(udonBehaviour);

                if (PrefabUtility.IsPartOfPrefabInstance(ctAsset))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(ctAsset);
                }
            }
            return dirty;
        }

        private static bool ApplyToUdon(IList<CyanTriggerAsset> ctAssets)
        {
            bool anyChanges = false;
            foreach (var ctAsset in ctAssets)
            {
                var program = ctAsset.assetInstance.cyanTriggerProgram as CyanTriggerEditableProgramAsset;
                if (program == null)
                {
                    continue;
                }
                
                anyChanges |= program.ApplyToUdon(ctAsset);
            }
            return anyChanges;
        }
        
        public bool TryVerifyAndMigrate()
        {
            bool dirty = false;

            dirty |= TryMigrateTrigger();
            dirty |= TryVerify();

            return dirty;
        }

        public bool TryVerify()
        {
            bool dirty = false;
#if CYAN_TRIGGER_DEBUG
            string path = AssetDatabase.GetAssetPath(this);
            if (path.Contains("DefaultCustomActions"))
            {
                // Always lock custom actions.
                if (!isLocked)
                {
                    Debug.Log($"Program set locked: {path}");
                    isLocked = true;
                    dirty = true;
                    EditorUtility.SetDirty(this);
                }

                if (!ctDataInstance.ignoreEventWarnings)
                {
                    Debug.Log($"Program set to ignore event warnings: {path}");
                    ctDataInstance.ignoreEventWarnings = true;
                    dirty = true;
                    EditorUtility.SetDirty(this);
                }

                // Guarantee Default Actions are None when not synced and Manual when synced.
                if (ctDataInstance.autoSetSyncMode == false 
                    || ctDataInstance.programSyncMode != CyanTriggerProgramSyncMode.ManualWithAutoRequest)
                {
                    Debug.Log($"Program with wrong sync: {ctDataInstance.autoSetSyncMode} {ctDataInstance.programSyncMode}, {path}");
                    
                    ctDataInstance.autoSetSyncMode = true;
                    ctDataInstance.programSyncMode = CyanTriggerProgramSyncMode.ManualWithAutoRequest;
                    
                    dirty = true;
                    EditorUtility.SetDirty(this);
                }
            }
#endif
            
            bool verifyDirty = CyanTriggerUtil.ValidateTriggerData(ctDataInstance);
            if (verifyDirty)
            {
                dirty = true;
#if CYAN_TRIGGER_DEBUG
                Debug.Log($"Setting CyanTrigger Program dirty after verification: {AssetDatabase.GetAssetPath(this)}");
#endif
                EditorUtility.SetDirty(this);
            }
            
            return dirty;
        }
        
        public bool TryMigrateTrigger()
        {
            int prevVersion = ctDataInstance.version;
            if (CyanTriggerVersionMigrator.MigrateTrigger(ctDataInstance))
            {
#if CYAN_TRIGGER_DEBUG
                Debug.Log($"Migrated CyanTrigger Program from version {prevVersion} to version {ctDataInstance.version}, {AssetDatabase.GetAssetPath(this)}");
#endif
                EditorUtility.SetDirty(this);

                return true;
            }

            return false;
        }

        public override string GetDefaultCyanTriggerProgramName()
        {
            return name;
        }
        
        #region Variable Verification

        /// <summary>
        /// Go through all CyanTriggerAssets and verify the variables are proper with the program.
        /// If the program variables were updated and the CTA does not match, update the variable list to ensure it matches.
        /// This will also update all prefab files if any CTA is on a prefab instance.
        /// </summary>
        /// <param name="triggerAssets"></param>
        /// <param name="allowPrefabInstances"></param>
        /// <returns>
        /// Returns if any changes were made to know if the scene should be dirtied and saved.
        /// </returns>
        public static bool VerifyVariablesAndApply(IList<CyanTriggerAsset> triggerAssets, bool allowPrefabInstances = true)
        {
            bool anyChanges = false;
            anyChanges |= VerifyAssetVariables(triggerAssets, allowPrefabInstances);
            anyChanges |= ApplyToUdon(triggerAssets);

            return anyChanges;
        }
        
        /// <summary>
        /// Note that this will verify only the variables on non prefab instance CyanTriggerAssets.
        /// This is needed to help with order of operations when updating prefab instance variables that the lowest level
        /// prefab version has the variables in the correct order before processing a prefab instance.
        /// </summary>
        /// <param name="prefabPath"></param>
        private static void VerifyVariablesForPrefab(string prefabPath)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefab == null)
            {
                return;
            }
            
            if (PrefabUtility.IsPartOfImmutablePrefab(prefab))
            {
                PrefabUtility.UnloadPrefabContents(prefab);
                return;
            }
            
            if (VerifyVariablesAndApply(prefab.GetComponentsInChildren<CyanTriggerAsset>(true), false))
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(prefab);
        }
        
        public static bool VerifyAssetVariables(
            IList<CyanTriggerAsset> ctAssets, 
            bool allowPrefabInstances = true)
        {
            bool anyChanges = false;
            List<CyanTriggerAsset> ctAssetPrefabInstances = new List<CyanTriggerAsset>();
            foreach (var ctAsset in ctAssets)
            {
                var program = ctAsset.assetInstance.cyanTriggerProgram as CyanTriggerEditableProgramAsset;
                if (program == null)
                {
                    continue;
                }
                
                ForceGuidPrefabOverride(ctAsset);
                
                var variables = program.ctDataInstance.variables;
            
                if (!DoesAssetNeedUpdating(ctAsset, variables))
                {
                    continue;
                }
                
                if (PrefabUtility.IsPartOfPrefabInstance(ctAsset))
                {
                    if (allowPrefabInstances)
                    {
                        ctAssetPrefabInstances.Add(ctAsset);
                    }
                }
                else
                {
                    UpdateCyanTriggerAssetVariables(ctAsset, variables);
                    anyChanges = true;
                }
            }

            // Batch all prefab related modifications since this involves modifying all prefab files.
            if (ctAssetPrefabInstances.Count > 0)
            {
                VerifyPrefabAssetVariables(ctAssetPrefabInstances);
                anyChanges = true;
            }

            return anyChanges;
        }

        // Ensure that each data element contains data that corresponds to the proper type for the variable.
        private static bool VerifyAssetVariableValuesMatchType(
            CyanTriggerAsset ctAsset,
            CyanTriggerVariable[] variables)
        {
            var assetInstance = ctAsset.assetInstance;
            var varData = assetInstance.variableData;

            bool changes = false;
            if (varData == null)
            {
                varData = new CyanTriggerSerializableObject[variables.Length];
                changes = true;
            }

            if (varData.Length != variables.Length)
            {
                changes = true;
                Array.Resize(ref varData, variables.Length);
            }

            for (int index = 0; index < variables.Length; ++index)
            {
                var variable = variables[index];
                var type = variable.type.Type;
                if (varData[index] == null)
                {
                    varData[index] =
                        new CyanTriggerSerializableObject(CyanTriggerPropertyEditor.GetDefaultForType(type));
                    changes = true;
                    continue;
                }

                bool badData = false;
                var data = CyanTriggerPropertyEditor.CreateInitialValueForType(type, varData[index].Obj, ref badData);
                if (badData)
                {
                    varData[index].Obj = data;
                    changes = true;
                }
            }

            if (changes)
            {
#if CYAN_TRIGGER_DEBUG
                Debug.LogWarning($"CyanTriggerAsset had variable data mismatch: {VRC.Tools.GetGameObjectPath(ctAsset.gameObject)}, {ctAsset.gameObject.scene.path}");
#endif
                Undo.RecordObject(ctAsset, Undo.GetCurrentGroupName());
                assetInstance.variableData = varData;
                
                if (PrefabUtility.IsPartOfPrefabInstance(ctAsset))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(ctAsset);
                }
            }

            return changes;
        }

        private static bool DoesAssetNeedUpdating(CyanTriggerAsset ctAsset, CyanTriggerVariable[] variables)
        {
            // Verify data stored in the ct asset matches the program. 
            var assetInstance = ctAsset.assetInstance;
            var varData = assetInstance.variableData;
            var varGuids = assetInstance.variableGuids;

            if (varData == null
                || varGuids == null
                || varData.Length != varGuids.Length
                || varData.Length != variables.Length)
            {
                return true;
            }

            for (int index = 0; index < variables.Length; ++index)
            {
                if (variables[index].variableID != varGuids[index])
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetPropertyIndexFromPrefabModPath(string path)
        {
            int openBrakLoc = path.IndexOf('[');
            int endBrakLoc = path.IndexOf(']');
            if (openBrakLoc == -1 || endBrakLoc == -1)
            {
                return -1;
            }

            ++openBrakLoc;
            if (!int.TryParse(path.Substring(openBrakLoc, endBrakLoc - openBrakLoc), out int results))
            {
                results = -1;
            }

            return results;
        }

        // Helper class just to store data related to remapping variables for a specific program.
        private class ProgramVariableRemapData
        {
            public readonly string VariableSize;
            public readonly CyanTriggerVariable[] Variables;
            public readonly Dictionary<string, int> GuidRemap;

            public ProgramVariableRemapData(CyanTriggerEditableProgramAsset program)
            {
                Variables = program.ctDataInstance.variables;
                VariableSize = Variables.Length.ToString();
                GuidRemap = new Dictionary<string, int>();
                for (var index = 0; index < Variables.Length; ++index)
                {
                    var variable = Variables[index];
                    GuidRemap.Add(variable.variableID, index);
                }
            }
        }
        
        private static void VerifyPrefabAssetVariables(IList<CyanTriggerAsset> ctAssets)
        {
            if (ctAssets == null || ctAssets.Count == 0)
            {
                return;
            }

            Dictionary<string, ProgramVariableRemapData> programToRemapData =
                new Dictionary<string, ProgramVariableRemapData>();

            string assetInstancePath = nameof(CyanTriggerAsset.assetInstance);
            string variableDataPath =
                $"{assetInstancePath}.{nameof(CyanTriggerAssetSerializableInstance.variableData)}";
            string variableGuidsPath =
                $"{assetInstancePath}.{nameof(CyanTriggerAssetSerializableInstance.variableGuids)}";
            string guidArraySizePath = $"{variableGuidsPath}.Array.size";
            string dataArraySizePath = $"{variableDataPath}.Array.size";

            // Go through prefab modifications that match this CyanTriggerAsset program
            // for each data modification, find associated guid modification update data modification's index
            bool ProcessCyanTriggerAssetPrefabChanges(
                CyanTriggerAsset processingAsset,
                List<PropertyModification> ctMods,
                List<PropertyModification> allMods)
            {
                var program = processingAsset.assetInstance.cyanTriggerProgram as CyanTriggerEditableProgramAsset;
                if (program == null)
                {
                    // No changes, just add all mods back to the list.
                    allMods.AddRange(ctMods);
                    return false;
                }
                
                string programPath = AssetDatabase.GetAssetPath(program);
                if (!programToRemapData.TryGetValue(programPath, out var remapData))
                {
                    remapData = new ProgramVariableRemapData(program);
                    programToRemapData.Add(programPath, remapData);   
                }
                
                Dictionary<int, PropertyModification> guids = new Dictionary<int, PropertyModification>();
                // CyanTriggerSerializedObject encodes multiple items
                Dictionary<int, List<PropertyModification>> data = new Dictionary<int, List<PropertyModification>>();
                
                PropertyModification dataArraySizeMod = null;
                PropertyModification guidArraySizeMod = null;
                bool foundChanges = false;
                
                // Go through each modification for this CyanTriggerAsset
                // Map all data and guid mods based on array index
                foreach (var modification in ctMods)
                {
                    string propertyPath = modification.propertyPath;
                    if (propertyPath.StartsWith(variableDataPath))
                    {
                        int index = GetPropertyIndexFromPrefabModPath(propertyPath);
                        if (index != -1)
                        {
                            if (!data.TryGetValue(index, out var mods))
                            {
                                mods = new List<PropertyModification>();
                                data.Add(index, mods);
                            }

                            mods.Add(modification);
                        }
                        else
                        {
#if CYAN_TRIGGER_DEBUG
                            if (dataArraySizeMod != null)
                            {
                                Debug.LogWarning("Unexpected prefab modification");
                            }
#endif
                            dataArraySizeMod = modification;
                        }
                    }
                    else if (propertyPath.StartsWith(variableGuidsPath))
                    {
                        int index = GetPropertyIndexFromPrefabModPath(propertyPath);
                        if (index != -1)
                        {
                            guids[index] = modification;

                            foundChanges |= index >= remapData.Variables.Length ||
                                            remapData.Variables[index].variableID != modification.value;
                        }
                        else
                        {
#if CYAN_TRIGGER_DEBUG
                            if (guidArraySizeMod != null)
                            {
                                Debug.LogWarning("Unexpected prefab modification");
                            }
#endif
                            guidArraySizeMod = modification;
                        }
                    }
                    else
                    {
                        // Other fields should be added directly
                        allMods.Add(modification);
                    }
                }

                // Go through each data index and remap the PropertyModifications to the new index.
                foreach (var dataChanges in data)
                {
                    int index = dataChanges.Key;
                    var dataMods = dataChanges.Value;

                    // If a guid doesn't have an index but data does, this is a problem.
                    // This shouldn't happen in normal situations as all Guids should be saved.
                    
                    // This can happen when adding a prefab that has not properly updated variables.
                    // On adding, variables that have been moved with a different type in the new place will become
                    // overrides, but the guid array will not.
                    // These items will end up being removed anyway.
                    // Unsure if this case is bad or not, but ignore these data values as they cannot be added anywhere.
                    if (!guids.TryGetValue(index, out var guidMod))
                    {
                        foundChanges = true;
                        continue;
                    }

                    // Check if Guid still exists. If not, ignore these mods as they were removed.
                    if (!remapData.GuidRemap.TryGetValue(guidMod.value, out int newIndex))
                    {
                        // Guid doesn't exist, so force changes. 
                        foundChanges = true;
                        continue;
                    }
                    
                    // At this point we know that we have a matching data mod with guid mod.
                    // Update the index for the data and guid mods
                    
                    // Add back the guid mod with the new index.
                    allMods.Add(
                        new PropertyModification
                        {
                            target = processingAsset,
                            value = guidMod.value,
                            propertyPath = $"{variableGuidsPath}.Array.data[{newIndex}]",
                        }
                    );

                    // If new index is the same as the old, do nothing and just add the mods back with no changes.
                    if (newIndex == index)
                    {
                        foreach (var mod in dataMods)
                        {
                            allMods.Add(mod);
                        }
                        continue;
                    }

                    // We know data has a new index, update the paths with the new index.
                    foundChanges = true;

                    string GetUpdatedPath(string path)
                    {
                        int openBrakLoc = path.IndexOf('[');
                        int endBrakLoc = path.IndexOf(']');
                        string front = path.Substring(0, openBrakLoc + 1);
                        string end = path.Substring(endBrakLoc);
                        return $"{front}{newIndex}{end}";
                    }

                    foreach (var mod in dataMods)
                    {
                        string newPath = GetUpdatedPath(mod.propertyPath);
                        mod.propertyPath = newPath;
                        allMods.Add(mod);
                    }
                }

                foundChanges |= guidArraySizeMod == null 
                                || guidArraySizeMod.value != remapData.VariableSize 
                                || dataArraySizeMod == null 
                                || dataArraySizeMod.value != remapData.VariableSize;
                
                // Required in the case where a prefab value is set in an index out of bounds for the data or guid arrays.
                allMods.Add(
                    new PropertyModification
                    {
                        target = processingAsset,
                        value = remapData.VariableSize,
                        propertyPath = guidArraySizePath
                    }
                );
                allMods.Add(
                    new PropertyModification
                    {
                        target = processingAsset,
                        value = remapData.VariableSize,
                        propertyPath = dataArraySizePath
                    }
                );
                
                return foundChanges;
            }

            CyanTriggerPrefabMigrator.StartExternalProcessing();
            
            // Go through all CyanTriggerAssets and find all prefab instance roots related to it, including those in prefab assets.
            // All instance roots will have Prefab Modifications that will need to be updated, in the scene and in prefab files.
            // Dependency sort it such that the asset files are processed before dependencies and scene instances. 
            CyanTriggerPrefabInstanceDependency dependencies = new CyanTriggerPrefabInstanceDependency();
            foreach (var ctAsset in ctAssets)
            {
                dependencies.AddPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(ctAsset));
            }
            List<GameObject> prefabList = dependencies.GetOrder();
            
            foreach (var prefabRoot in prefabList)
            {
                var prefabModifications = PrefabUtility.GetPropertyModifications(prefabRoot);
                // If there are no prefab modifications, then this must be a prefab file root.
                // Verify the prefab's CyanTriggerAssets for all non prefab based variable updates. 
                if (prefabModifications == null)
                {
                    string path = AssetDatabase.GetAssetPath(prefabRoot);
                    if (!string.IsNullOrEmpty(path))
                    {
                        VerifyVariablesForPrefab(path);
                        AssetDatabase.SaveAssets();
                    }
                    continue;
                }
                
                Dictionary<CyanTriggerAsset, List<PropertyModification>> modsPerAsset =
                    new Dictionary<CyanTriggerAsset, List<PropertyModification>>();
                    
                List<PropertyModification> modifications = new List<PropertyModification>();

                // Go through all modifications and save all CyanTriggerAsset mods
                foreach (var modification in prefabModifications)
                {
                    if (modification.target is CyanTriggerAsset asset)
                    {
                        if (!modsPerAsset.TryGetValue(asset, out var mods))
                        {
                            mods = new List<PropertyModification>();
                            modsPerAsset.Add(asset, mods);
                        }
                        mods.Add(modification);
                    }
                    else
                    {
                        modifications.Add(modification);
                    }
                }
                
                bool changes = false;
                foreach (var ctMods in modsPerAsset)
                {
                    changes |= ProcessCyanTriggerAssetPrefabChanges(ctMods.Key, ctMods.Value, modifications);
                }

                if (changes)
                {
                    PrefabUtility.SetPropertyModifications(prefabRoot, modifications.ToArray());
                    
                    AssetDatabase.SaveAssets();
                }
            }
            
            CyanTriggerPrefabMigrator.FinishExternalProcessing();
        }

        // Go through all variable data and guids, pair them together, and then reorder them based on the provided variables
        private static void UpdateCyanTriggerAssetVariables(CyanTriggerAsset ctAsset, CyanTriggerVariable[] variables)
        {
            int variableCount = variables.Length;

            var assetInstance = ctAsset.assetInstance;
            var varGuids = assetInstance.variableGuids;

            Undo.RecordObject(ctAsset, Undo.GetCurrentGroupName());

            // This case shouldn't happen normally, but may happen when migrating old data. 
            // Assume the data is "correct" and only update guid order. 
            if (varGuids == null)
            {
                varGuids = assetInstance.variableGuids = new string[variableCount];
                for (int index = 0; index < variableCount; ++index)
                {
                    varGuids[index] = variables[index].variableID;
                }

                if (PrefabUtility.IsPartOfPrefabInstance(ctAsset))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(ctAsset);
                }
                return;
            }
            
            var varData = assetInstance.variableData;
            
            // Data is miss-matched. Try to recreate align.
            Dictionary<string, CyanTriggerSerializableObject> idToData =
                new Dictionary<string, CyanTriggerSerializableObject>();

            if (varData == null)
            {
                varData = assetInstance.variableData = Array.Empty<CyanTriggerSerializableObject>();
            }

            // Match the data to each guid for easy look up.
            if (varData.Length == varGuids.Length)
            {
                for (int index = 0; index < varGuids.Length; ++index)
                {
                    string id = varGuids[index];
                    if (!string.IsNullOrEmpty(id))
                    {
                        idToData.Add(id, varData[index]);
                    }
                }
            }

            // Update the arrays to be the proper length given the actual variables. 
            Array.Resize(ref assetInstance.variableData, variableCount);
            Array.Resize(ref assetInstance.variableGuids, variableCount);

            varData = assetInstance.variableData;
            varGuids = assetInstance.variableGuids;

            // Rearrange data to match actual variable guid orders. 
            for (int index = 0; index < variableCount; ++index)
            {
                var variable = variables[index];
                string id = variable.variableID;
                varGuids[index] = id;
                if (!idToData.TryGetValue(id, out varData[index]))
                {
                    bool _ = false;
                    var data = CyanTriggerPropertyEditor.CreateInitialValueForType(variable.type.Type, variable.data.Obj, ref _);
                    varData[index] = new CyanTriggerSerializableObject(data);
                }
            }

            if (PrefabUtility.IsPartOfPrefabInstance(ctAsset))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(ctAsset);
            }
        }

        public static Dictionary<CyanTriggerEditableProgramAsset, List<CyanTriggerAsset>>
            GetProgramsToCyanTriggerAssets(IList<CyanTriggerAsset> triggerAssets)
        {
            Dictionary<CyanTriggerEditableProgramAsset, List<CyanTriggerAsset>> programs =
                    new Dictionary<CyanTriggerEditableProgramAsset, List<CyanTriggerAsset>>();
            foreach (var triggerAsset in triggerAssets)
            {
                var program = triggerAsset.assetInstance?.cyanTriggerProgram;
                if (program == null || !(program is CyanTriggerEditableProgramAsset ctProgram))
                {
                    continue;
                }

                if (!programs.TryGetValue(ctProgram, out List<CyanTriggerAsset> ctAssets))
                {
                    ctAssets = new List<CyanTriggerAsset>();
                    programs.Add(ctProgram, ctAssets);
                }

                ctAssets.Add(triggerAsset);
            }

            return programs;
        }

        public static void ForceGuidPrefabOverride(IList<CyanTriggerAsset> triggerAssets)
        {
            Dictionary<CyanTriggerEditableProgramAsset, List<CyanTriggerAsset>> programs =
                GetProgramsToCyanTriggerAssets(triggerAssets);

            foreach (var ctAssets in programs.Values)
            {
                ForceGuidPrefabOverrideSameProgram(ctAssets, false);
            }
        }
        
        private static void ForceGuidPrefabOverrideSameProgram(IList<CyanTriggerAsset> triggerAssets, bool skipVerification)
        {
            if (!skipVerification)
            {
                bool initialized = false;
                CyanTriggerEditableProgramAsset asset = null;
                foreach (var ctAssets in triggerAssets)
                {
                    var program = (CyanTriggerEditableProgramAsset)ctAssets.assetInstance.cyanTriggerProgram;
                    if (!initialized)
                    {
                        initialized = true;
                        asset = program;
                        continue;
                    }

                    if (program != asset)
                    {
                        Debug.LogError("[CyanTrigger] Cannot force GUID override in CyanTrigger assets as provided list do not all have the same program.");
                        return;
                    }
                }
            }

            UnityEngine.Object[] ctAssetsObject = new UnityEngine.Object[triggerAssets.Count];
            for (int index = 0; index < triggerAssets.Count; ++index)
            {
                ctAssetsObject[index] = triggerAssets[index];
            }
            SerializedObject ctSerialized = new SerializedObject(ctAssetsObject);
            SerializedProperty assetInstanceProperty = ctSerialized.FindProperty(nameof(CyanTriggerAsset.assetInstance));
            SerializedProperty variableGuidsProperty = assetInstanceProperty.FindPropertyRelative(nameof(CyanTriggerAssetSerializableInstance.variableGuids));
            SerializedProperty variableDataProperty = assetInstanceProperty.FindPropertyRelative(nameof(CyanTriggerAssetSerializableInstance.variableData));

            ForceGuidPrefabOverride(triggerAssets, variableGuidsProperty, variableDataProperty);
        }

        public static void ForceGuidPrefabOverride(CyanTriggerAsset triggerAsset)
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(triggerAsset))
            {
                return;
            }
            
            SerializedObject ctSerialized = new SerializedObject(triggerAsset);
            SerializedProperty assetInstanceProperty = ctSerialized.FindProperty(nameof(CyanTriggerAsset.assetInstance));
            SerializedProperty variableGuidsProperty = assetInstanceProperty.FindPropertyRelative(nameof(CyanTriggerAssetSerializableInstance.variableGuids));
            SerializedProperty variableDataProperty = assetInstanceProperty.FindPropertyRelative(nameof(CyanTriggerAssetSerializableInstance.variableData));

            ForceGuidPrefabOverride(new List<CyanTriggerAsset> {triggerAsset}, variableGuidsProperty, variableDataProperty);
        }

        public static void ForceGuidPrefabOverride(
            IList<CyanTriggerAsset> ctAssets,
            SerializedProperty variableGuidsProperty, 
            SerializedProperty variableDataProperty)
        {
            if (!variableGuidsProperty.isInstantiatedPrefab)
            {
                return;
            }
            
            int size = variableGuidsProperty.arraySize;
            bool needsUpdating = false;
            if (variableGuidsProperty.prefabOverride || variableDataProperty.prefabOverride)
            {
                for (int index = 0; index < size; ++index)
                {
                    try
                    {
                        SerializedProperty dataProp = variableDataProperty.GetArrayElementAtIndex(index);
                        SerializedProperty guidProp = variableGuidsProperty.GetArrayElementAtIndex(index);
                        
                        if (dataProp.prefabOverride && !guidProp.prefabOverride)
                        {
                            needsUpdating = true;
                        }
                    }
                    catch (Exception)
                    {
#if CYAN_TRIGGER_DEBUG
                        System.Text.StringBuilder paths = new System.Text.StringBuilder("Misaligned Asset Paths:\n");
                        foreach (var asset in ctAssets)
                        {
                            paths.Append(VRC.Tools.GetGameObjectPath(asset.gameObject));
                            paths.Append(", ");
                            paths.AppendLine(asset.gameObject.scene.path);
                        }
                        Debug.LogWarning(paths.ToString());
#endif
                        needsUpdating = true;
                        // In cases where the array size property was modified, but the array is smaller,
                        // this will throw an exception. Try/Catch here just to prevent crashing here.
                    }
                }
            }

            if (!needsUpdating)
            {
                return;
            }
            
            
            string sizeString = size.ToString();
            
            string guidArrayPropPath = variableGuidsProperty.propertyPath;
            string dataArrayPropPath = variableDataProperty.propertyPath;
            // The array itself doesnt actually hold the property.
            string guidArrayStartPath = $"{guidArrayPropPath}.Array.";
            string guidArraySizePath = $"{guidArrayStartPath}size";
            string dataArraySizePath = $"{dataArrayPropPath}.Array.size";
            
            foreach (var ctAsset in ctAssets)
            {
                CyanTriggerAsset prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(ctAsset);
                
                if (prefabAsset == null)
                {
                    continue;
                }
                
                List<PropertyModification> modifications = new List<PropertyModification>();
                
                // TODO make generic method for adding these properties and share it with the verify method above.
                // Required in the case where a prefab value is set in an index out of bounds for the data or guid arrays.
                modifications.Add(
                    new PropertyModification
                    {
                        target = prefabAsset,
                        value = sizeString,
                        propertyPath = guidArraySizePath
                    }
                );
                
                modifications.Add(
                    new PropertyModification
                    {
                        target = prefabAsset,
                        value = sizeString,
                        propertyPath = dataArraySizePath
                    }
                );

                var overrides = PrefabUtility.GetPropertyModifications(ctAsset);
                if (overrides != null)
                {
                    Dictionary<int, PropertyModification> guids = new Dictionary<int, PropertyModification>();
                    Dictionary<int, List<PropertyModification>> data = new Dictionary<int, List<PropertyModification>>();
                    
                    // Go through all modifications, and save all guid and data mods for this prefab asset.
                    // Data will be matched to guid, or if no guid exists, new guid modification will be created.
                    foreach (var modification in overrides)
                    {
                        // Ignore non CyanTriggerAsset modifications
                        // Or items that are not for the expected CyanTriggerAsset.
                        if (!(modification.target is CyanTriggerAsset modAsset) || modAsset != prefabAsset)
                        {
                            modifications.Add(modification);
                            continue;
                        }

                        string propertyPath = modification.propertyPath;
                        if (propertyPath.StartsWith(dataArrayPropPath))
                        {
                            int index = GetPropertyIndexFromPrefabModPath(propertyPath);
                            if (index != -1)
                            {
                                if (!data.TryGetValue(index, out var mods))
                                {
                                    mods = new List<PropertyModification>();
                                    data.Add(index, mods);
                                }

                                mods.Add(modification);
                                modifications.Add(modification);
                            }
                        }
                        else if (propertyPath.StartsWith(guidArrayPropPath))
                        {
                            int index = GetPropertyIndexFromPrefabModPath(propertyPath);
                            if (index != -1)
                            {
                                guids[index] = modification;
                            }
                        }
                        else
                        {
                            modifications.Add(modification);
                        }
                    }
                    
                    // Match Data modifications to guids, or create new if unmatched.
                    foreach (var keyValues in data)
                    {
                        int index = keyValues.Key;

                        // Add guid mod directly
                        if (guids.TryGetValue(index, out var guidMod))
                        {
                            modifications.Add(guidMod);
                        }
                        // Create new guid modification for this index.
                        else
                        {
                            try
                            {
                                SerializedProperty guidProp = variableGuidsProperty.GetArrayElementAtIndex(index);
                                modifications.Add(
                                    new PropertyModification
                                    {
                                        target = prefabAsset,
                                        value = guidProp.stringValue,
                                        propertyPath = guidProp.propertyPath,
                                    }
                                );
                            }
                            catch (Exception)
                            {
                                // In cases where the array size property was modified, but the array is smaller,
                                // this will throw an exception. Try/Catch here just to prevent crashing here.
                            }
                        }
                    }
                }

                PrefabUtility.SetPropertyModifications(ctAsset, modifications.ToArray());
            }
        }

        // Used for debugging
        public static void LogOverrides(CyanTriggerAsset[] ctAssets)
        {
            foreach (var ctAsset in ctAssets)
            {
                var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(ctAsset);
                if (prefabAsset == null)
                {
                    continue;
                }
                
                var overrides = PrefabUtility.GetPropertyModifications(ctAsset);
                if (overrides != null)
                {
                    foreach (var mod in overrides)
                    {
                        if (mod.target is CyanTriggerAsset)
                        {
                            Debug.Log($"{mod.propertyPath}, {mod.value}, {mod.objectReference}");
                        }
                    }
                }
            }
        }

        #endregion
    }
}