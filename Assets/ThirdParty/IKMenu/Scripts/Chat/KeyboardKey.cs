
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class KeyboardKey : UdonSharpBehaviour
{
    public string lowCharacter;
    public string upCharacter;
    public string altCharacter;
    private string character;
    public ChatManager manager;
    public TextMeshProUGUI buttonText;

    public void Start()
    {
        //_SetLower();
    }

    public void _PressKey()
    {
        manager._SendKey(character);
    }

    public void _SetLower()
    {
        character = lowCharacter;
        buttonText.text = character;
    }

    public void _SetUpper()
    {
        if (!string.IsNullOrWhiteSpace(upCharacter))
        {
            character = upCharacter;
            buttonText.text = character;
        }
    }

    public void _SetAlt()
    {
        if (!string.IsNullOrWhiteSpace(altCharacter))
        {
            character = altCharacter;
            buttonText.text = character;
        }
    }
}
