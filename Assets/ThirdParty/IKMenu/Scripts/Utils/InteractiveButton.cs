
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class InteractiveButton : UdonSharpBehaviour
{
    public UdonBehaviour target;
    public string eventName;
    public bool networked = false;

    public override void Interact()
    {
        if(target != null && !string.IsNullOrEmpty(eventName))
        {
            if (networked)
                target.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, eventName);
            else
                target.SendCustomEvent(eventName);
        }
    }
}
