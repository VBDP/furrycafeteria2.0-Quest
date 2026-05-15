
using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class PostProcessSlider : UdonSharpBehaviour
{
    public Slider slider;
    public PostProcessVolume volume;
    public bool invert;

    public void _SliderMoved()
    {
        volume.weight = invert ? 1 - slider.value : slider.value;
    }
}
