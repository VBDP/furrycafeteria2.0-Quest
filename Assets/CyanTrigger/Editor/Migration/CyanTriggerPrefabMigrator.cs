using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Cyan.CT.Editor
{
    public class CyanTriggerPrefabMigrator : AssetPostprocessor
    {
        private static bool _delayedMigrationStarted = false;
        private static bool _migrationInProgress = false;
        private static int _externalProcessing = 0;

        private static List<string> _skippedImportedAssets = null;

        public static void StartExternalProcessing()
        {
            ++_externalProcessing;
        }
        
        public static void FinishExternalProcessing()
        {
            --_externalProcessing;
        }
        
        public static void MigrateAllPrefabs()
        {
            if (_delayedMigrationStarted)
            {
                return;
            }
            
            EditorApplication.update += DelayedMigrateAllPrefabs;
        }

        private static void DelayedMigrateAllPrefabs()
        {
            if (_migrationInProgress || _externalProcessing > 0)
            {
                return;
            }
            
            EditorApplication.update -= DelayedMigrateAllPrefabs;
            
            if (_delayedMigrationStarted)
            {
                return;
            }

            var settings = CyanTriggerSettings.Instance;
            
            // If settings assumes current project migration version is equal to the data version,
            // then no need to check all prefabs for migration. 
            // This doesn't catch all cases, but will at least get the majority of the prefabs in a project. 
            // Migration will still happen on importing a prefab or opening a scene or prefab manually.
            if (settings.lastMigratedDataVersion >= CyanTriggerDataInstance.DataVersion)
            {
                _delayedMigrationStarted = true;
                // Check for any assets not processed on original import
                if (_skippedImportedAssets != null)
                {
                    string[] importedAssets = _skippedImportedAssets.ToArray();
                    _skippedImportedAssets = null;
                    OnPostprocessAllAssets(importedAssets, null, null, null);
                }
                return;
            }
            
            // Only log project migration when a previous data version was set.
            if (settings.lastMigratedDataVersion != CyanTriggerSettingsData.DefaultMigrationDataVersionValue)
            {
                Debug.Log($"[CyanTrigger] Migrating project Prefabs and Programs to current data version: {CyanTriggerDataInstance.DataVersion}");
            }
            
            CyanTriggerSerializedProgramManager.CompileAllCyanTriggerEditableAssets(true);
            MigratePrefabs(CyanTriggerPrefabDependency.GetValidPrefabPaths());

            settings.lastMigratedDataVersion = CyanTriggerDataInstance.DataVersion;
            EditorUtility.SetDirty(settings);
            
            _delayedMigrationStarted = true;
            _skippedImportedAssets = null;
        }
        
        // Verify prefabs and Programs on import.
        private static void OnPostprocessAllAssets(
            string[] importedAssets, 
            string[] deletedAssets, 
            string[] movedAssets, 
            string[] movedFromAssetPaths)
        {
            if (_migrationInProgress || _externalProcessing > 0)
            {
                return;
            }

            if (!_delayedMigrationStarted)
            {
                // Skip this import check but save in the case where project does not need full migration.
                if (_skippedImportedAssets == null)
                {
                    _skippedImportedAssets = new List<string>();
                }
                _skippedImportedAssets.AddRange(importedAssets);
                return;
            }
            
            VerifyImportedProgramAssets(importedAssets);
            MigratePrefabs(importedAssets);
        }
        
        // TODO make coroutine to prevent locking the main thread. 
        private static void MigratePrefabs(IList<string> paths)
        {
            _migrationInProgress = true;

            CyanTriggerPrefabDependency dependencies = new CyanTriggerPrefabDependency();
            foreach (var path in paths)
            {
                dependencies.AddAsset(path, true);
            }

            VerifyAndMigratePrefabs(dependencies);
            
            _migrationInProgress = false;
        }

        public static void VerifyAndMigratePrefabs(CyanTriggerPrefabDependency dependencies)
        {
            var sortedOrder = dependencies.GetOrder();
            if (sortedOrder.Count == 0)
            {
                return;
            }
            
            try
            {
                AssetDatabase.StartAssetEditing();
                
                foreach (var prefabData in sortedOrder)
                {
                    // Ignore scenes
                    if (!prefabData.IsPrefab)
                    {
                        continue;
                    }
                    
                    string path = prefabData.Path;

                    string fileText = File.ReadAllText(Path.GetFullPath(path));

                    // This is really hacky, but speedups processing considerably :upsidedown:
                    // Basically checks if the file has a specific component in it and ignores files that do not.
                    
                    // CyanTrigger
                    bool hasCyanTrigger =
                        fileText.Contains("m_Script: {fileID: 11500000, guid: 3dd4a7956009f7d429a09b8371329c82, type: 3}");
                    // CyanTriggerAsset
                    bool hasCyanTriggerAsset =
                        fileText.Contains("m_Script: {fileID: 11500000, guid: 7dbcb0ee0db04e7298f72e639d9e2588, type: 3}");

                    // Does not have either, so ignore this prefab.
                    if (!hasCyanTrigger && !hasCyanTriggerAsset)
                    {
                        continue;
                    }
                    
                    GameObject prefab = PrefabUtility.LoadPrefabContents(path);
                    if (PrefabUtility.IsPartOfImmutablePrefab(prefab))
                    {
                        PrefabUtility.UnloadPrefabContents(prefab);
                        continue;
                    }

                    bool anyChanges = false;
                    if (hasCyanTrigger)
                    {
                        anyChanges |= CyanTriggerSerializerManager.VerifySceneTriggersUnderGameObject(prefab);
                    }

                    if (hasCyanTriggerAsset)
                    {
                        anyChanges |= CyanTriggerSerializerManager.VerifyCyanTriggerAssetsUnderGameObject(prefab);
                    }
                    
                    if (anyChanges)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefab, path);
                    }

                    PrefabUtility.UnloadPrefabContents(prefab);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        // When importing CyanTrigger Programs, the compiled program is not imported and will contain old data
        // This check goes through all new imported programs to verify if the compiled version matches or if it needs to be recompiled. 
        private static void VerifyImportedProgramAssets(string[] importedAssets)
        {
            _migrationInProgress = true;
            
            List<CyanTriggerEditableProgramAsset> programToCompile = new List<CyanTriggerEditableProgramAsset>();
            foreach (string path in importedAssets)
            {
                CyanTriggerEditableProgramAsset program =
                    AssetDatabase.LoadAssetAtPath<CyanTriggerEditableProgramAsset>(path);
                if (program != null 
                    && !program.HasErrors() 
                    && (program.TryVerifyAndMigrate() || !program.SerializedProgramHashMatchesExpectedHash()))
                {
                    programToCompile.Add(program);
                }
            }

            if (programToCompile.Count > 0)
            {
                CyanTriggerSerializedProgramManager.CompileCyanTriggerEditableAssetsAndDependencies(programToCompile);
            }
            
            _migrationInProgress = false;
        }
    }
}