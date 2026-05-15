using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class IKMenuSetup
{
    private struct OrderLayer
    {
        public string name;
        public uint uniqueID;
    }

    private static OrderLayer[] layers =
    {
        new OrderLayer{name = "Default", uniqueID = 0},
        new OrderLayer{name = "Nameplates", uniqueID = 2450873877},
        new OrderLayer{name = "UI", uniqueID = 7711475},
        new OrderLayer{name = "Popup", uniqueID = 3286212389}
    };

    [MenuItem("Tools/Setup IKMenu Layers")]
    [InitializeOnLoadMethod]
    private static void SetupMenu()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty layersProp = tagManager.FindProperty("m_SortingLayers");

        while(layersProp.arraySize < layers.Length)
        {
            layersProp.InsertArrayElementAtIndex(layersProp.arraySize);
        }

        for (int i = 1; i < layers.Length; i++)
        {
            layersProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue = layers[i].name;
            layersProp.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID").longValue = layers[i].uniqueID;
        }

        if (tagManager.hasModifiedProperties)
        {
            tagManager.ApplyModifiedProperties();
            EditorUtility.SetDirty(tagManager.targetObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
