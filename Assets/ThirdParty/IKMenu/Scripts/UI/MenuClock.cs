
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MenuClock : UdonSharpBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    private void Update()
    {
        DateTime time = DateTime.Now;

        string hour = time.Hour.ToString().PadLeft(2, '0');
        string minute = time.Minute.ToString().PadLeft(2, '0');

        text.text = hour + ":" + minute;
    }
}
