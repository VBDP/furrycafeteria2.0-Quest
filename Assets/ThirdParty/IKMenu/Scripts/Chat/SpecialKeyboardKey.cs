
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SpecialKeyboardKey : UdonSharpBehaviour
{
    public KeyCode key;
    public ChatManager manager;
    public GameObject activeBg;

    private void Start()
    {
        if(activeBg != null) activeBg.SetActive(false);
    }

    public void _PressKey()
    {
        switch (key)
        {
            case KeyCode.Space: manager._SendKey(" "); break;
            case KeyCode.Tab: manager._SendKey("    "); break;
            case KeyCode.Return: manager._Enter(); break;
            case KeyCode.Backspace: manager._Backspace(); break;
            case KeyCode.CapsLock:
                if(manager.caps) manager._CapsOff();
                else manager._CapsOn();
                activeBg.SetActive(manager.caps);
                break;
            case KeyCode.LeftShift:
                if (manager.shift) manager._ShiftOff();
                else manager._ShiftOn();
                activeBg.SetActive(manager.shift);
                break;
            case KeyCode.AltGr:
                if (manager.altgr) manager._AltOff();
                else manager._AltOn();
                activeBg.SetActive(manager.altgr);
                break;
            default: break;
        }
    }

    public void _SetActive(bool active)
    {
        activeBg.SetActive(active);
    }
}
