
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class Toggle2DPlayer : UdonSharpBehaviour
{
    [SerializeField] private Image iconOff;
    [SerializeField] private Image iconOn;
    public bool isOn = true;
    [SerializeField] private GameObject[] onObjects;
    [SerializeField] private GameObject[] offObjects;
    [SerializeField] private AudioSource playerSource;
    [SerializeField] private AudioSource playerSource2D;
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

    public void _ToggleOn()
    {
        isOn = true;
        _UpdateObjects();
        audioSource.Play();
    }

    public void _ToggleOff()
    {
        isOn = false;
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

        foreach (GameObject onObject in onObjects)
        {
            onObject.SetActive(isOn);
        }
        foreach (GameObject offObject in offObjects)
        {
            offObject.SetActive(!isOn);
        }

        playerSource.mute = isOn;
        playerSource2D.mute = !isOn;
    }
}
