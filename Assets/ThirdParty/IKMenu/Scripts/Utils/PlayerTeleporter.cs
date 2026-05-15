
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerTeleporter : UdonSharpBehaviour
{
    public Transform target;

    public void _Teleport()
    {
        if (target)
            Networking.LocalPlayer.TeleportTo(target.position, target.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, false);

    }
}
