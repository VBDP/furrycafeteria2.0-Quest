
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class MapPlayersDisplay : UdonSharpBehaviour
{
    [SerializeField]private RectTransform mapRoot;
    [SerializeField]private GameObject mapPin;
    public Vector2 offset = Vector2.zero;
    public float scale = 1;

    private RectTransform[] pins;
    private VRCPlayerApi[] players;

    void Start()
    {
        UpdatePlayers(null);
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        UpdatePlayers(null);
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        UpdatePlayers(player);
    }

    private void OnEnable()
    {
        UpdatePlayers(null);
    }

    private void UpdatePlayers(VRCPlayerApi ignorePlayer)
    {
        if (!enabled)
            return;

        players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        players = VRCPlayerApi.GetPlayers(players);
        pins = new RectTransform[players.Length];

        for (int i = mapRoot.childCount-1; i >= 0; i--)
            Destroy(mapRoot.GetChild(i).gameObject);

        for (int i = 0; i < pins.Length; i++)
        {
            if (Utilities.IsValid(players[i]) && players[i] != ignorePlayer)
            {
                pins[i] = Instantiate(mapPin).GetComponent<RectTransform>();
                pins[i].SetParent(mapRoot);
                pins[i].localPosition = Vector3.zero;
                pins[i].localRotation = Quaternion.identity;
                pins[i].localScale = Vector3.one;
            
                pins[i].gameObject.name = players[i].playerId.ToString();
                pins[i].GetChild(0).GetComponent<TextMeshProUGUI>().text = players[i].displayName;
                if(players[i].isLocal)
                    pins[i].GetComponent<Image>().color = Color.green;
            }
        }
    }

    private void LateUpdate()
    {
        if (pins.Length > 0)
        {
            for (int i = 0; i < pins.Length; i++)
            {
                if (pins[i] != null && Utilities.IsValid(players[i]))
                {
                    Vector3 pos = players[i].GetPosition()* scale;
                    pins[i].localPosition = new Vector3(pos.x+ offset.x, pos.z+ offset.y, 0);
                }
            }
        }
    }
}
