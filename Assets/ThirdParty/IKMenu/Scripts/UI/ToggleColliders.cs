
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class ToggleColliders : UdonSharpBehaviour
{
    [SerializeField] private Image iconOff;
    [SerializeField] private Image iconOn;
    public bool isOn = true;
    [SerializeField] private Collider[] onObjects;
    [SerializeField] private Collider[] offObjects;
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

        foreach (Collider onObject in onObjects)
        {
            onObject.enabled = isOn;
        }
        foreach (Collider offObject in offObjects)
        {
            offObject.enabled = !isOn;
        }
    }
}
