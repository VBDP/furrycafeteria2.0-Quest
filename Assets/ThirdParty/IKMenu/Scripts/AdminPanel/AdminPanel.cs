
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

[DefaultExecutionOrder(-10000)]
public class AdminPanel : UdonSharpBehaviour
{
    [SerializeField] private RectTransform playersRoot;
    [SerializeField] private GameObject playerButton;
    [SerializeField] private AdminSystem adminSystem;
    [SerializeField] private GameObject functions;
    [SerializeField] private ToggleButton dangerActions;
    [SerializeField] private TextMeshProUGUI logsText;
    [Space()]
    [SerializeField] private TextAsset adminList;

    private string cachedAdminList;
    private VRCPlayerApi targetPlayer;
    [SerializeField] private TextAsset key;
    private Toggle[] playerButtons;
    private VRCPlayerApi[] players;

    private int selectedPlayer = -1;
    private int selectedPlayerId = -1;

    void Start()
    {
        cachedAdminList = adminList.text;
        functions.SetActive(false);
    }

    private void OnEnable()
    {
        UpdatePlayerList(null);
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        logsText.text += "<color=#49ff71>" + player.displayName + "[" + string.Format(player.IsUserInVR() ? "VR" : "Desktop") + "] - Joined</color>\n";
        //logsText.text += "<color=#49ff71>"+player.displayName + " - Joined</color>\n";
        UpdatePlayerList(null);
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        logsText.text += "<color=#ff4965>"+player.displayName + " - Left</color>\n";
        UpdatePlayerList(player);
    }

    private void UpdatePlayerList(VRCPlayerApi ignoredPlayer)
    {
        if (!gameObject.activeInHierarchy) return;
        
        selectedPlayer = -1;
        if(functions!=null) functions.SetActive(false);
        dangerActions._ToggleOff();

        if (!enabled)
            return;

        players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        players = VRCPlayerApi.GetPlayers(players);
        playerButtons = new Toggle[players.Length];

        for (int i = playersRoot.childCount - 1; i >= 0; i--)
            Destroy(playersRoot.GetChild(i).gameObject);

        for (int i = 0; i < playerButtons.Length; i++)
        {
            VRCPlayerApi player = players[i];

            if (Utilities.IsValid(players[i]) && player != ignoredPlayer)
            {
                playerButtons[i] = Instantiate(playerButton).GetComponent<Toggle>();
                playerButtons[i].transform.SetParent(playersRoot);
                playerButtons[i].transform.localPosition = Vector3.zero;
                playerButtons[i].transform.localRotation = Quaternion.identity;
                playerButtons[i].transform.localScale = Vector3.one;

                playerButtons[i].gameObject.name = player.playerId.ToString();
                playerButtons[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = player.displayName;
                //playerButtons[i].transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = $"id:{player.playerId} - type:{(player.IsUserInVR() ? "VR" : "Desktop")}";
            }
        }

        SendCustomEventDelayedFrames(nameof(_RefreshSelectedPlayer),2);
    }

    public void _RefreshSelectedPlayer()
    {
        if (playerButtons.Length > 0)
        {
            selectedPlayer = -1;

            for (int i = 0; i < playerButtons.Length; i++)
            {
                if(playerButtons[i] && playerButtons[i].gameObject.name == selectedPlayerId.ToString())
                {
                    playerButtons[i].SetIsOnWithoutNotify(true);
                    selectedPlayer = 1;
                }
            }

            if(selectedPlayer == -1)
                selectedPlayerId = -1;
            
            functions.SetActive(selectedPlayer != -1);
        }
    }

    public void _SelectedPlayerChanged()
    {
        dangerActions._ToggleOff();

        if (playerButtons.Length > 0)
        {
            selectedPlayer = -1;
            for (int i = 0; i < playerButtons.Length; i++)
            {
                if (playerButtons[i] != null && Utilities.IsValid(players[i]) && playerButtons[i].isOn)
                {
                    selectedPlayer = i;
                    selectedPlayerId = players[i].playerId;
                    _SetTargetPlayer(players[i]);
                    break;
                }
            }
            
            functions.SetActive(selectedPlayer != -1);
        }
    }

    #region admin functions

    #region Utility

    private void _SetTargetPlayer(VRCPlayerApi player)
    {
        targetPlayer = player;
    }

    private string Rc4(string input, string rc4key)
    {
        if (rc4key == null)
        {
            VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount() + 5];
            VRCPlayerApi.GetPlayers(players);
            foreach (VRCPlayerApi player in players)
            {
                if (Utilities.IsValid(player) && player.isMaster)
                {
                    rc4key = player.displayName + key;
                    break;
                }
            }
        }

        string result = "";
        int x, y, j = 0;
        int[] box = new int[256];

        for (int i = 0; i < 256; i++)
        {
            box[i] = i;
        }

        for (int i = 0; i < 256; i++)
        {
            j = (rc4key[i % rc4key.Length] + box[i] + j) % 256;
            x = box[i];
            box[i] = box[j];
            box[j] = x;
        }

        for (int i = 0; i < input.Length; i++)
        {
            y = i % 256;
            j = (box[y] + j) % 256;
            x = box[y];
            box[y] = box[j];
            box[j] = x;

            result += (char)(input[i] ^ box[(box[y] + box[j]) % 256]);
        }
        return result;
    }

    private bool CheckAllowedAdmin(string playerName)
    {
        if (adminList.text != cachedAdminList)
        {
            //Debug.LogError("Cached admin list doesn't correspond");
            return false;
        }

        //Name Spoofing test (with actual admin in room)
        VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount() + 5];
        VRCPlayerApi.GetPlayers(players);

        string[] playerNames = new string[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            if (Utilities.IsValid(players[i]))
                playerNames[i] = players[i].displayName;
            else
                playerNames[i] = "AAplayer_" + i;
        }

        int matches = 0;
        for (int i = 0; i < playerNames.Length; i++)
        {
            if (playerNames[i] == Networking.LocalPlayer.displayName || playerNames[i] == "Anonymous")
                matches++;

            if (matches > 1)
            {
                //Debug.LogError("Player with same name as caller present");
                return false;
            }
        }

        for (int i = 0; i < playerNames.Length; i++)
        {
            for (int j = 0; j < playerNames.Length; j++)
            {
                if (i != j && playerNames[i] == playerNames[j])
                {
                    //Debug.LogError("2 Players with same name present");
                    return false;
                }
            }
        }

        string[] separator = new string[] { "\r\n", "\n", "\r" };
        string[] list = adminList.text.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        foreach (string admin in list)
        {
            if (admin == playerName)
                return true;
        }

        //Debug.LogError("Other error admin");
        return false;
    }
    #endregion

    private void _CallMethod(string methodName, string parameters)
    {
        //Debug.Log("Call Method " + methodName);
        if (CheckAllowedAdmin(Networking.LocalPlayer.displayName))
        {
            string target = "_";
            if (Utilities.IsValid(targetPlayer))
                target = targetPlayer.displayName;

            Networking.SetOwner(Networking.LocalPlayer, adminSystem.gameObject);

            string syncedCall = Rc4("¤" + Networking.LocalPlayer.playerId + "/" + Networking.LocalPlayer.displayName + "/" + methodName + "/" + target + "/" + UnityEngine.Random.Range(0, 10000) + "/" + parameters, null);
            adminSystem._SetSyncedCall(syncedCall);
        }
    }

    public void _KickPlayer() { _CallMethod("kick", ""); }
    public void _FreezePlayer() { _CallMethod("freeze", ""); }
    public void _UnFreezePlayer() { _CallMethod("unfreeze", ""); }
    public void _MutePlayer() { _CallMethod("mute", ""); }
    public void _UnMutePlayer() { _CallMethod("unmute", ""); }
    public void _Warn() { _CallMethod("warn", ""); }
    public void _HideAvatar() { _CallMethod("avatar", ""); }
    public void _DropPickups() { _CallMethod("drop", ""); }
    public void _DropAll() { _CallMethod("dropall", ""); }
    public void _RespawnAll() { _CallMethod("respawnallpicks", ""); }
    public void _Teleport() { _CallMethod("teleport", ""); }
    public void _Bring() { _CallMethod("bring", ""); }
    public void _Spy() { _CallMethod("spy", ""); }
    public void _StopSpy() { adminSystem._StopSpy(); }
    public void _ClearChat() { _CallMethod("clearchat", ""); }
    public void _TPAdminIsland() { _CallMethod("TPAdminIsland", ""); }
    public void _TPPrespawnIsland() { _CallMethod("TPPrespawnIsland", ""); }
    #endregion
}
