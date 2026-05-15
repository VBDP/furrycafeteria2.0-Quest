
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class NotificationDestroyer : UdonSharpBehaviour
{
    public void _Destroy()
    {
        Destroy(gameObject);
    }
}
