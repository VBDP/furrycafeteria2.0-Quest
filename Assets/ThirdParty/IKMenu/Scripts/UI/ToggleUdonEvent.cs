
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using System.Collections.Generic;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
#endif

public class ToggleUdonEvent : UdonSharpBehaviour
{
    [SerializeField] private Image iconOff;
    [SerializeField] private Image iconOn;
    public bool isOn = true;
    [SerializeField] private UdonSharpBehaviour[] behaviour;
    [SerializeField] private string onEvent;
    [SerializeField] private string offEvent;
    private AudioSource audioSource;


    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        _UpdateObjects();
    }

    public void _ToggleChange()
    {
        isOn = !isOn;
        _UpdateObjects();
        audioSource.Play();
    }

    private void _UpdateObjects()
    {
        Color baseColor = iconOn.color;
        baseColor.a = isOn ? 1.0f : 0.25f;
        iconOn.color = baseColor;

        baseColor = iconOff.color;
        baseColor.a = isOn ? 0.25f : 1.0f;
        iconOff.color = baseColor;

        if (isOn)
        {
            foreach (UdonSharpBehaviour udonBehaviour in behaviour)
            {
                udonBehaviour.SendCustomEvent(onEvent);
            }
        }
        else
        {
            foreach (UdonSharpBehaviour udonBehaviour in behaviour)
            {
                udonBehaviour.SendCustomEvent(offEvent);
            }
        }
    }
}
