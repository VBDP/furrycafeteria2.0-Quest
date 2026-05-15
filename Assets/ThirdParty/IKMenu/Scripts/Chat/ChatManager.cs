
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ChatManager : UdonSharpBehaviour
{
    [SerializeField] private ChatDisplay chatDisplay;
    [SerializeField] private HUDSystem hudSystem;
    [SerializeField] private InputField chatInput;
    [SerializeField] private GameObject inputObject;
    [SerializeField] private Dropdown layoutInput;
    [SerializeField] private Toggle notificationToggle;
    [SerializeField] private KeyboardKey[] keys;
    [SerializeField] private SpecialKeyboardKey[] shiftKeys;
    [SerializeField] private SpecialKeyboardKey[] altKeys;
    [SerializeField] private SpecialKeyboardKey capsKeys;
    [SerializeField] private int maxLines = 30;
    [SerializeField] private int maxChars = 140;
    [Multiline] [SerializeField] private string[] layouts;

    public bool caps;
    public bool shift;
    public bool altgr;

    private string storedAccent = "";
    private string vowels = "aeiou";
    private string accentChars = "aeioun";
    private string vowelsCircumflex = "âêîôû ";
    private string vowelsUmlaut = "äëïöü ";
    private string vowelsGrave = "àèìòù ";
    private string vowelsTilde = "ã  õ ñ";

    public string allowedChars;
    public bool inChat;

    [UdonSynced, FieldChangeCallback(nameof(SyncedChat))]
    private string _syncedChat;

    private void Start()
    {
        if (chatInput == null)
        {
            if (inputObject == null)
            {
                inputObject = transform.parent.Find("Panels/Chat/Input").gameObject;
            }

            if(inputObject != null)
                chatInput = inputObject.GetComponent<InputField>();
        }

        if (layouts == null)
        {
            layouts = new string[]
            {
                "²&é\"'(-è_çà)=azertyuiop^$qsdfghjklmù*<wxcvbn,;:! 1234567890°+AZERTYUIOP¨£QSDFGHJKLM%µ>WXCVBN?./§  ~#{[|`\\^@]}  €        ¤                       ",
                "`1234567890-=qwertyuiop[]asdfghjkl;'\\\\zxcvbnm,./~!@#$%^&*()_+QWERTYUIOP{}ASDFGHJKL:\"||ZXCVBNM<>?                                                ",
                "§1234567890'^qwertzuiopè¨asdfghjkléà$<yxcvbnm,.-°+\"*ç%&/()=?`QWERTZUIOPü!ASDFGHJKLöä£>YXCVBNM;:_ ¦@#°§¬|¢  ´~  €       []          {}\\          "
            };
        }

        chatInput.characterLimit = maxChars;
        _ChangeLayout();

        allowedChars = "";
        foreach (string layout in layouts)
        {
            allowedChars += layout;
        }
    }

    private void Update()
    {
        VRCPlayerApi player = Networking.LocalPlayer;

        if (chatDisplay.gameObject.activeInHierarchy && 
            player.isLocal && 
            !player.IsUserInVR())
        {
            if(Input.GetKeyDown(KeyCode.Return))
                _Enter();

            if (inChat != chatInput.isFocused)
            {
                inChat = chatInput.isFocused;
                player.Immobilize(inChat);

                if(inChat)
                    player.SetJumpImpulse(0);
                else
                    player.SetJumpImpulse(3);
            }

        }
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (Networking.IsOwner(gameObject))
        {
            RequestSerialization();
        }
    }

    private string SyncedChat
    {
        set
        {
            _syncedChat = value;
            //chatScreen.text = _syncedChat;
            string[] messages = _syncedChat.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (notificationToggle.isOn && !Networking.IsOwner(gameObject))
            {
                hudSystem._ShowNotification(0,messages[messages.Length-1]);
            }

            chatDisplay._UpdateChat(messages);
        }
        get => _syncedChat;
    }

    public void _SendKey(string character)
    {
        if (!string.IsNullOrEmpty(character) && (chatInput.text.Length+character.Length) < 280)
        {
            if (character == "^" || character == "¨" || character == "~" || character == "`")
            {
                if (!string.IsNullOrEmpty(storedAccent))
                {
                    chatInput.text += storedAccent[0] + character;
                    storedAccent = "";
                }
                else
                    storedAccent = character;
            }
            else
            {
                if (!string.IsNullOrEmpty(storedAccent))
                {
                    if ((storedAccent != "~" && vowels.Contains(character.ToLower())) || (storedAccent=="~" && "aon".Contains(character.ToLower())))
                    {
                        bool isUp = char.IsUpper(character[0]);

                        int index = accentChars.IndexOf(character.ToLower(), StringComparison.Ordinal);

                        switch (storedAccent)
                        {
                            case "^":
                                character = vowelsCircumflex[index] + string.Empty;
                                break;
                            case "¨":
                                character = vowelsUmlaut[index] + string.Empty;
                                break;
                            case "`":
                                character = vowelsGrave[index] + string.Empty;
                                break;
                            case "~":
                                character = vowelsTilde[index] + string.Empty;
                                break;
                        }

                        if (isUp) character = character.ToUpper();
                    }
                    else
                    {
                        chatInput.text += storedAccent[0];
                    }

                    storedAccent = "";
                }
                chatInput.text += character;
            }

            chatInput.text = chatInput.text.Substring(0, Mathf.Min(maxChars, chatInput.text.Length));

            if (shift) _ShiftOff();
            if (altgr) _AltOff();
            foreach (SpecialKeyboardKey key in shiftKeys) key._SetActive(false);
            foreach (SpecialKeyboardKey key in altKeys) key._SetActive(false);
        }
    }
    
    public void _CapsOn()
    {
        caps = true;
        shift = false;
        altgr = false;
        _SetKeysUpper();
        foreach (SpecialKeyboardKey key in shiftKeys) key._SetActive(false);
        foreach (SpecialKeyboardKey key in altKeys) key._SetActive(false);
    }
    
    public void _CapsOff()
    {
        caps = false;
        altgr = false;
        if (!shift)
        {
            _SetKeysLower();
        }
    }
    
    public void _ShiftOn()
    {
        shift = true;
        altgr = false;
        _SetKeysUpper();
        foreach (SpecialKeyboardKey key in shiftKeys) key._SetActive(true);
        foreach (SpecialKeyboardKey key in altKeys) key._SetActive(false);
    }
    
    public void _ShiftOff()
    {
        shift = false;
        altgr = false;
        if (!caps)
        {
            _SetKeysLower();
        }
        foreach (SpecialKeyboardKey key in shiftKeys) key._SetActive(false);
    }

    public void _AltOn()
    {
        altgr = true;
        shift = false;
        caps = false;
        _SetKeysAlt();
        foreach (SpecialKeyboardKey key in shiftKeys) key._SetActive(false);
        capsKeys._SetActive(false);
    }

    public void _AltOff()
    {
        altgr = false;
        shift = false;
        caps = false;
        _SetKeysLower();
    }

    public void _Backspace()
    {
        if (chatInput.text.Length > 0)
        {
            chatInput.text = chatInput.text.Remove(chatInput.text.Length - 1);
        }
    }

    public void _Enter()
    {
        if (chatInput.text.Length > 0 && !string.IsNullOrWhiteSpace(chatInput.text))
        {
            string msg = "";
            foreach (char chr in chatInput.text)
            {
                if(allowedChars.IndexOf(chr) != -1)
                    msg += chr;
            }

            if (msg.Length > maxChars)
                msg = msg.Substring(0, maxChars);
            chatInput.text = "";

            string chat = SyncedChat + "\n[" + Networking.LocalPlayer.displayName + "|"+DateTime.Now.ToString("HH:mm")+"]:/" + msg;
            string[] lines = chat.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            chat = "";

            int totalLines = Mathf.Min(lines.Length, maxLines);

            for (int i = Mathf.Max(lines.Length- totalLines,0); i < lines.Length; i++)
                chat += lines[i] + "\n";

            Networking.SetOwner(Networking.LocalPlayer,gameObject);
            SyncedChat = chat;
            RequestSerialization();
        }
    }

    private void _SetKeysUpper()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i]._SetUpper();
        }
    }

    private void _SetKeysLower()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i]._SetLower();
        }
    }

    private void _SetKeysAlt()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i]._SetAlt();
        }
    }

    public void _ChangeLayout()
    {
        string selectedLayout = layouts[layoutInput.value];

        for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
        {
            keys[keyIndex].lowCharacter = selectedLayout.Substring(keyIndex, 1);
            keys[keyIndex].upCharacter = selectedLayout.Substring(keyIndex+keys.Length, 1);
            keys[keyIndex].altCharacter = selectedLayout.Substring(keyIndex + keys.Length*2, 1);
        }

        _SetKeysLower();
    }

    public string RemoveLineEndings(string value)
    {
        if (String.IsNullOrEmpty(value))
        {
            return value;
        }
        string lineSeparator = ((char)0x2028).ToString();
        string paragraphSeparator = ((char)0x2029).ToString();

        return value.Replace("\r\n", string.Empty)
            .Replace("\n", string.Empty)
            .Replace("\r", string.Empty)
            .Replace(lineSeparator, string.Empty)
            .Replace(paragraphSeparator, string.Empty);
    }

    public string[] _GetChat()
    {
        return _syncedChat.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
    }

    public void _ClearChat()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        SyncedChat = "";
        RequestSerialization();
    }
}
