
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using TMPro;
using VRC.SDK3.Components;
using VRC.SDK3.StringLoading;
using VRC.Udon.Common.Interfaces;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
#endif

[DefaultExecutionOrder(-10001)]
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class AdminSystem : UdonSharpBehaviour
{
    [SerializeField] private TextAsset adminList;
    [SerializeField] private TextAsset bannedList;
    [SerializeField] private GameObject[] map;
    [SerializeField] private Material skyMat;
    [SerializeField] private GameObject[] autoDestroy;
    [SerializeField] private GameObject[] autoDestroyAdmin;
    [SerializeField] private Collider[] ColliderDestroy;
    [SerializeField] private TextMeshProUGUI result;
    [SerializeField] private HUDSystem hud;
    [SerializeField] private Transform mapRoot;
    [SerializeField] private GameObject screen;
    [SerializeField] private ChatManager chatManager;
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private GameObject prefab;
    [SerializeField] private GameObject FlySystem;
    [SerializeField] private GameObject[] chairs;
    [SerializeField] private GameObject tpIslaAdmin;
    [SerializeField] private GameObject tpIslaPreSpawn;

    private VRC_AvatarPedestal avatarPedestal;
    private string cachedAdminList;
    [UdonSynced, FieldChangeCallback(nameof(SyncedCall))]
    private string _syncedCall;
    [SerializeField] private TextAsset key;
    private VRCObjectSync[] objSync;
    private VRCPlayerApi spyPlayer;
    private Camera cam;

    private string dynamicBannedList ="";
    private string[] cachedBannedList;

    
    [Header("Action Texts")]
    [SerializeField] private string freezeText = "An admin froze you";
    [SerializeField] private string unfreezeText = "An admin unfroze you";
    [SerializeField] private string muteText = "An admin muted you";
    [SerializeField] private string unmuteText = "An admin unmuted you";
    [SerializeField] private string warnText = "You got warned, be careful with your behavior";

    [SerializeField] private VRCUrl AdminListURL;
    public float ReloadDelay = 60f;
    public String AdminPage;

    public void _DownloadList()
    {
        VRCStringDownloader.LoadUrl(AdminListURL, (IUdonEventReceiver)this);
        SendCustomEventDelayedSeconds(nameof(_DownloadList),ReloadDelay);
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        AdminPage = result.Result;
    }

    public override void OnStringLoadError(IVRCStringDownload result)
    {
        Debug.Log(result.Error);
    }

    private void Start()
    {
        
        _DownloadList();
        cachedAdminList = adminList.text;

        MeshRenderer rend = GetComponent<MeshRenderer>();

        _TestBan(Networking.LocalPlayer);
        //_TestHardBan(Networking.LocalPlayer);
        
        if (Utilities.IsValid(Networking.LocalPlayer))
        {
            if (_CheckAllowedAdmin(Networking.LocalPlayer.displayName))
            {
                GameObject camObj = Instantiate(mainCamera);
                cam = camObj.GetComponent<Camera>();
                cam.nearClipPlane = 0.15f;
                cam.farClipPlane = 200;
                cam.cullingMask = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 4) | (1 << 8) | (1 << 9) | (1 << 11) |
                                  (1 << 13) | (1 << 14) | (1 << 17) | (1 << 18) | (1 << 22) | (1 << 23);
                cam.transform.SetParent(screen.transform);
                cam.targetTexture = (RenderTexture)screen.GetComponent<MeshRenderer>().sharedMaterial.mainTexture;

                foreach (GameObject o in autoDestroyAdmin)
                {
                    Destroy(o);
                }
            }
            else
            {
                foreach (GameObject o in autoDestroy)
                {
                    Destroy(o);
                }
                foreach(Collider c in ColliderDestroy)
                {
                    Destroy(c);
                }
            }
            
        }

        /*VRCPickup[] pickups = (VRCPickup[])mapRoot.GetComponentsInChildren(typeof(VRCPickup));
        objSync = new VRCObjectSync[pickups.Length];
        for (int i = 0; i < pickups.Length; i++)
        {
            objSync[i] = (VRCObjectSync)pickups[i].gameObject.GetComponent(typeof(VRCObjectSync));
        }*/
    }

    public override void PostLateUpdate()
    {
        if (spyPlayer != null)
        {
            if (Utilities.IsValid(spyPlayer))
            {
                VRCPlayerApi.TrackingData tracking = spyPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                cam.transform.position = tracking.position;
                cam.transform.rotation = tracking.rotation;
            }
        }
    }

    private void _UpdateCachedBanList()
    {
        string fullList = bannedList.text;

        if(dynamicBannedList.Length > 0)
            fullList += "\r\n" + dynamicBannedList;

        string[] separator = new string[] { "\r\n", "\n", "\r" };
        cachedBannedList = fullList.Split(separator, StringSplitOptions.RemoveEmptyEntries);
    }

    private void _TestBan(VRCPlayerApi player)
    {
        if (Utilities.IsValid(player))
        {
            if(cachedBannedList == null)
                _UpdateCachedBanList();

            bool isBanned = false;
            for (int nameIndex = 0; nameIndex < cachedBannedList.Length; nameIndex++)
            {
                if (cachedBannedList[nameIndex] == player.displayName)
                {
                    isBanned = true;
                    break;
                }
            }

            if (isBanned)
            {
                _KickPlayer(player);
                if (!player.isLocal && _CheckAllowedAdmin(Networking.LocalPlayer.displayName))
                {
                    hud._ShowNotification(2, $"Attention le joueur banni {player.displayName} a rejoins!");
                }
            }
        }
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        _TestBan(player);
        _TestBan(Networking.LocalPlayer);
    }

    public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
    {
        if(requestedOwner == null)
            return _CheckAllowedAdmin(requestingPlayer.displayName);

        if (requestedOwner.isLocal && !_CheckAllowedAdmin(requestedOwner.displayName))
            return true;

        return _CheckAllowedAdmin(requestedOwner.displayName);
    }

    /*private void OnDisable()
    {
        enabled = true;
        if(!gameObject.activeInHierarchy)
            gameObject.SetActive(true);
    }*/

    private string SyncedCall
    {
        set
        {
            _syncedCall = value;
            _ProcessCall(Rc4(value, null));
        }
        get => _syncedCall;
    }

    private void _ProcessCall(string receivedCall)
    {
        if(receivedCall[0] != '¤') return;

        receivedCall = receivedCall.Remove(0,1);

        char[] separator = new char[] { '/' };
        string[] callParams = receivedCall.Split(separator);

        int playerId = -1;

        Debug.Log(receivedCall);
        Debug.Log($"{callParams[0]}");

        if (callParams.Length >= 5 && int.TryParse(callParams[0], out playerId) && _CheckAllowedAdmin(callParams[1]))
        {
            VRCPlayerApi playerFromId = VRCPlayerApi.GetPlayerById(playerId);
            /*if(Utilities.IsValid(playerFromId))
                Debug.Log($"{playerId} : {playerFromId.displayName}");
            else
                Debug.Log($"{playerId} : INVALID");*/

            if (Utilities.IsValid(playerFromId) && playerFromId.displayName == callParams[1] && _CheckAllowedAdmin(playerFromId.displayName))
            {
                VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount() + 5];
                VRCPlayerApi.GetPlayers(players);
                foreach (VRCPlayerApi player in players)
                {
                    if (Utilities.IsValid(player) && player.displayName == callParams[1])
                    {
                        //Debug.Log("Received Valid Call : " + receivedCall);
                        _ExecuteMethod(callParams[2], callParams[3], callParams[5], playerFromId);
                    }
                }
            }
        }
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
                    rc4key = player.displayName+key.text;
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

    public bool _CheckAllowedAdmin(string playerName)
    {

        if (adminList.text != cachedAdminList)
        {
            Debug.LogError("Cached admin list doesn't correspond");
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
                playerNames[i] = "AAplayer_"+i;
        }

        int matches = 0;
        for (int i = 0; i < playerNames.Length; i++)
        {
            if (playerNames[i] == Networking.LocalPlayer.displayName || playerNames[i] == "Anonymous")
                matches++;

            if (matches > 1)
            {
                Debug.LogError("Player with same name as caller present");
                return false;
            }
        }

        for (int i = 0; i < playerNames.Length; i++)
        {
            for (int j = 0; j < playerNames.Length; j++)
            {
                if (i != j && playerNames[i] == playerNames[j])
                {
                    Debug.LogError("2 Players with same name present");
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

        Debug.LogError("Other error admin");
        return false;
    }

    private VRCPlayerApi _GetPlayerByName(string playerName)
    {
        VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount() + 5];
        VRCPlayerApi.GetPlayers(players);

        foreach (VRCPlayerApi player in players)
        {
            if (Utilities.IsValid(player) && player.displayName == playerName)
            {
                return player;
            }
        }

        return null;
    }

    private void _ExecuteMethod(string method, string target, string parameters, VRCPlayerApi instigator)
    {
        if(result)
            result.text = instigator.displayName + " called " + method + " on " + target + (string.IsNullOrEmpty(parameters)? "" : " with ") + parameters;

        VRCPlayerApi player = _GetPlayerByName(target);

        switch (method)
        {
            case "kick": 
                if (Utilities.IsValid(player))
                {
                    _KickPlayer(player);
                }
                break;
            case "freeze":
                if (target == Networking.LocalPlayer.displayName)
                {
                    Networking.LocalPlayer.Immobilize(true);
                    hud._ShowNotification(1,freezeText);
                    FlySystem.SetActive(false);

                    foreach (var chair in chairs)
                    {
                        chair.SetActive(false);
                    }
                }
                break;
            case "unfreeze":
                if (target == Networking.LocalPlayer.displayName)
                {
                    Networking.LocalPlayer.Immobilize(false);
                    hud._ShowNotification(0, unfreezeText);
                    FlySystem.SetActive(true);
                    foreach (var chair in chairs)
                    {
                        chair.SetActive(true);
                    }
                }
                break;
            case "mute":
                if (Utilities.IsValid(player))
                {
                    if (target != Networking.LocalPlayer.displayName)
                    {
                        player.SetVoiceGain(0);
                        player.SetVoiceDistanceNear(0);
                        player.SetVoiceDistanceFar(0);
                        player.SetVoiceVolumetricRadius(0);
                    }
                    else
                    {
                        hud._ShowNotification(1, muteText);
                    }
                    
                }
                break;
            case "unmute":
                if (Utilities.IsValid(player))
                {
                    if (target != Networking.LocalPlayer.displayName)
                    {
                        player.SetVoiceGain(15);
                        player.SetVoiceDistanceNear(0);
                        player.SetVoiceDistanceFar(25);
                        player.SetVoiceVolumetricRadius(0);
                    }
                    else
                    {
                        hud._ShowNotification(0, unmuteText);
                    }
                }
                break;
            case "warn":
                if (target == Networking.LocalPlayer.displayName)
                {
                    hud._ShowNotification(1, warnText);
                }
                break;
            case "avatar":
                if (target == Networking.LocalPlayer.displayName)
                {
                    if (Utilities.IsValid(player))
                    {
                        if (!avatarPedestal)
                        {
                            avatarPedestal = Instantiate(prefab).GetComponent<VRC_AvatarPedestal>();
                        }

                        SendCustomEventDelayedFrames(nameof(_Change), 5);
                    }
                }
                break;
            case "drop":
                if (target == Networking.LocalPlayer.displayName)
                {
                    if (Utilities.IsValid(player))
                    {
                        VRC_Pickup pickup = player.GetPickupInHand(VRC_Pickup.PickupHand.Left);
                        if(pickup) pickup.Drop();
                        pickup = player.GetPickupInHand(VRC_Pickup.PickupHand.Right);
                        if (pickup) pickup.Drop();
                    }
                }
                break;
            case "teleport":
                if (instigator.isLocal && Utilities.IsValid(player))
                {
                    instigator.TeleportTo(player.GetPosition(), player.GetRotation(), VRC_SceneDescriptor.SpawnOrientation.Default, false);
                }
                break;
            case "bring":
                if (target == Networking.LocalPlayer.displayName)
                {
                    if (Utilities.IsValid(player) && Utilities.IsValid(instigator))
                    {
                        Networking.LocalPlayer.TeleportTo(instigator.GetPosition(), instigator.GetRotation(), VRC_SceneDescriptor.SpawnOrientation.Default, false);
                    }
                }
                break;
            case "spy":
                if (instigator.isLocal && Utilities.IsValid(player))
                {
                    if (spyPlayer == player)
                    {
                        spyPlayer = null;
                        screen.SetActive(false);
                        cam.gameObject.SetActive(false);
                        cam.enabled = false;
                    }
                    else
                    {
                        spyPlayer = player;
                        screen.SetActive(true);
                        cam.gameObject.SetActive(true);
                        cam.enabled = true;
                    }
                    
                }
                break;
            case "dropall":
                {
                    VRC_Pickup pickup = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left);
                    if (pickup) pickup.Drop();
                    pickup = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
                    if (pickup) pickup.Drop();
                    break;
                }
            case "respawnallpicks":
                if (instigator.isLocal)
                {
                    foreach (VRCObjectSync objectSync in objSync)
                    {
                        Networking.SetOwner(Networking.LocalPlayer, objectSync.gameObject);
                        objectSync.Respawn();
                    }
                }
                break;
            case "clearchat":
                if (instigator.isLocal)
                {
                    chatManager._ClearChat();
                }
                break;
            case "TPAdminIsland":
                if (instigator.isLocal)
                {
                    Networking.LocalPlayer.TeleportTo(tpIslaAdmin.transform.position, tpIslaAdmin.transform.rotation, VRC_SceneDescriptor.SpawnOrientation.Default, false);
                }
                break;
            case "TPPrespawnIsland":
                if (instigator.isLocal)
                {
                    Networking.LocalPlayer.TeleportTo(tpIslaPreSpawn.transform.position, tpIslaPreSpawn.transform.rotation, VRC_SceneDescriptor.SpawnOrientation.Default, false);
                }
                break;

        }
    }

    public void _Change()
    {
        if (avatarPedestal)
            avatarPedestal.SetAvatarUse(Networking.LocalPlayer);
    }

    private void _Kill(VRCPlayerApi player)
    {
        player.CombatSetCurrentHitpoints(0);
        player.SetAvatarAudioFarRadius(0.01f);
        player.SetAvatarAudioNearRadius(0.001f);
        player.SetVoiceGain(0);
    }

    private void _KickPlayer(VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            transform.SetParent(null);
            player.TeleportTo(new Vector3(50000, 50000, 50000), Quaternion.identity, VRC_SceneDescriptor.SpawnOrientation.Default, false);
            RenderSettings.skybox = skyMat;
            player.SetAvatarAudioGain(0);
            player.SetVoiceGain(0);

            foreach (GameObject obj in map)
            {
                Destroy(obj);
            }

            VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount() + 5];
            VRCPlayerApi.GetPlayers(players);
            foreach (VRCPlayerApi pl in players)
            {
                if (Utilities.IsValid(pl))
                {
                    pl.SetAvatarAudioGain(0);
                    pl.SetVoiceGain(0);
                    pl.SetVoiceDistanceNear(0);
                    pl.SetVoiceDistanceFar(0);
                    pl.SetVoiceVolumetricRadius(0);
                }
            }

        }
        else
        {
            player.SetAvatarAudioGain(0);
            player.SetVoiceGain(0);
            player.SetVoiceDistanceNear(0);
            player.SetVoiceDistanceFar(0);
            player.SetVoiceVolumetricRadius(0);
            player.CombatSetup();
            player.CombatSetRespawn(true, 9999999999999f, transform);
            player.CombatSetMaxHitpoints(100);
            player.CombatSetCurrentHitpoints(100);
            player.CombatSetCurrentHitpoints(0);
            
        }
    }

    public void _StopSpy()
    {
        spyPlayer = null;
        screen.SetActive(false);
        cam.enabled = false;
        cam.gameObject.SetActive(false);
    }

    public bool _IsSpying()
    {
        return Utilities.IsValid(spyPlayer);
    }

    public void _SetSyncedCall(string call)
    {
        Debug.Log("CCCC");
        SyncedCall = call;
        RequestSerialization();
        Debug.Log("Updated synced call");
    }

}
