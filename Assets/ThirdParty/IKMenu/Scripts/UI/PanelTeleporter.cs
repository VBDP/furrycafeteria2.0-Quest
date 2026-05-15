
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UdonSharpEditor;
#endif

public class PanelTeleporter : UdonSharpBehaviour
{
    public MenuController menuController;
    public Transform position;

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if(player != null && player == Networking.LocalPlayer &&  !menuController.pickup.IsHeld)
        {
            menuController.resetTarget = position;
            menuController._CloseMenu();
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // RootOnly update will only copy the data for this behaviour from Udon to the proxy
        //UdonSharpEditorUtility.CopyUdonToProxy(this);
        if (!menuController || !menuController.menu) return;

        Gizmos.matrix = Matrix4x4.TRS(position.position,position.rotation, position.localScale);
        Gizmos.DrawWireCube(Vector3.zero, menuController.menu.sizeDelta*menuController.menu.localScale.x);
    }
#endif
}
