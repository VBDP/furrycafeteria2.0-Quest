#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class IKMenuEditor : MonoBehaviour
{
    




    private void Start()
    {
        Transform shaderBlocker = transform.Find("PlayerTracker/Head/ShaderBlocker");
        if (shaderBlocker)
        {
            foreach(Transform child in shaderBlocker)
                child.gameObject.SetActive(false);
        }
    }
}


[CustomEditor(typeof(IKMenuEditor))]
[CanEditMultipleObjects]
public class IKMenuEditorInspector : Editor
{
    string dataPath = string.Empty;

    void OnEnable()
    {
        dataPath = IKMenuLocator.DataPath;
    }

    public override void OnInspectorGUI()
    {
        IKMenuEditor menuEditor = (IKMenuEditor)target;
    }
}
#endif