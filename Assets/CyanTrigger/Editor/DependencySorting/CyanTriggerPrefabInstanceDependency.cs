using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Cyan.CT.Editor
{
    public class CyanTriggerPrefabInstanceDependency
    {
        private readonly List<CyanTriggerDependency<GameObject>> _assets = 
            new List<CyanTriggerDependency<GameObject>>();
        private readonly Dictionary<string, CyanTriggerDependency<GameObject>> _assetMap = 
            new Dictionary<string, CyanTriggerDependency<GameObject>>();
        
        private static string GetPrefabKey(GameObject gameObject)
        {
            return $"{gameObject.scene.IsValid()} {gameObject.scene.path}, {VRC.Tools.GetGameObjectPath(gameObject)}, {PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject)}";
        }
        
        public void AddPrefabInstance(GameObject prefabRoot)
        {
            AddAssetInternal(GetPrefabKey(prefabRoot), prefabRoot);
        }
        
        private CyanTriggerDependency<GameObject> AddAssetInternal(string path, GameObject prefabRoot)
        {
            if (_assetMap.TryGetValue(path, out var dep))
            {
                return dep;
            }
            
            CyanTriggerDependency<GameObject> asset = new CyanTriggerDependency<GameObject>(prefabRoot);
            _assets.Add(asset);
            _assetMap.Add(path, asset);

            List<Transform> searchObjects = new List<Transform>();
            prefabRoot.GetComponentsInChildren(true, searchObjects);
        
            foreach (var obj in searchObjects)
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(obj.gameObject))
                {
                    var prefabVariant = obj.gameObject;
                    while (prefabVariant != null)
                    {
                        string depPath = GetPrefabKey(prefabVariant);
                        // Only check for dependencies on items that aren't itself.
                        if (prefabVariant != prefabRoot && path != depPath)
                        {
                            asset.AddDependency(AddAssetInternal(depPath, prefabVariant));
                        }
                        prefabVariant = PrefabUtility.GetCorrespondingObjectFromSource(prefabVariant);
                    }
                }
            }

            return asset;
        }
        
        public List<GameObject> GetOrder()
        {
            return CyanTriggerDependency<GameObject>.GetDependencyOrdering(_assets);
        }
    }
}