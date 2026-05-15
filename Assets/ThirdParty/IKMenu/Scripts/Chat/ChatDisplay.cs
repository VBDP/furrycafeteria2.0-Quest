using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ChatDisplay : UdonSharpBehaviour
{
    public ChatManager chatManager;
    public RectTransform chatRoot;
    public GameObject messagePrefab;

    private void OnEnable()
    {
        _UpdateChat(chatManager._GetChat());
    }

    public void _UpdateChat(string[] chat)
    {
        if (!gameObject.activeInHierarchy) return;

        /*int currentMsg = chatRoot.childCount;

        if (chat.Length == chatRoot.childCount && chatRoot.childCount>0)
        {
            Destroy(chatRoot.GetChild(0).gameObject);
            currentMsg--;
        }
        else if(chat.Length < chatRoot.childCount)
        {
            for (int i = chatRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(chatRoot.GetChild(i).gameObject);
            }

            currentMsg = 0;
        }

        currentMsg = Mathf.Max(currentMsg, 0);*/

        for (int i = chatRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(chatRoot.GetChild(i).gameObject);
        }

        for (int i = 0; i < chat.Length; i++)
        {
            int splitPoint = chat[i].IndexOf('/');
            string username = chat[i].Substring(0, splitPoint);
            string usertext = chat[i].Substring(splitPoint + 1);

            string msg = usertext;
            /*foreach (char chr in usertext)
            {
                if(chatManager.allowedChars.IndexOf(chr) != -1)
                    msg += chr;
            }*/

            GameObject message = Instantiate(messagePrefab);
            message.transform.localPosition = Vector3.zero;
            message.transform.localRotation = Quaternion.identity;
            message.transform.localScale = Vector3.one;
            message.transform.SetParent(chatRoot, false);
            message.GetComponent<TextMeshProUGUI>().text = username+msg;
        }
    }
}
