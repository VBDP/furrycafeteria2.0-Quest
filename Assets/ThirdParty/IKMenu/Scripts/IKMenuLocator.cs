using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class IKMenuLocator : ScriptableObject
{
    private static string _cachedLocation;

    public static string DataPath
    {
        get
        {
#if UNITY_EDITOR
            if (_cachedLocation != null)
                return _cachedLocation;

            string[] foundLocatorGuids = AssetDatabase.FindAssets($"t:{nameof(IKMenuLocator)}");
            List<IKMenuLocator> foundLocators = new List<IKMenuLocator>();

            foreach (string locatorGuid in foundLocatorGuids)
            {
                IKMenuLocator locator =
                    AssetDatabase.LoadAssetAtPath<IKMenuLocator>(AssetDatabase.GUIDToAssetPath(locatorGuid));

                if (locator)
                    foundLocators.Add(locator);
            }

            if (foundLocators.Count > 1)
                throw new System.Exception("Multiple IKMenu data locators found");

            if (foundLocators.Count== 0)
                throw new System.Exception("No IKMenu data locators found");

            _cachedLocation = Path.GetDirectoryName(AssetDatabase.GetAssetPath(foundLocators[0]));
            _cachedLocation = _cachedLocation.Substring(0, _cachedLocation.LastIndexOf('\\'));
            Debug.Log(_cachedLocation);
            return _cachedLocation;
#else
                throw new System.PlatformNotSupportedException("Cannot get IKMenu data path outside of the Editor runtime");
#endif
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(IKMenuLocator))]
internal class IKMenuLocatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Do not delete this file!", MessageType.Error);
    }
}
#endif
