using UnityEngine;

namespace AvatarNow.Decoder
{
    public static class SwitchDebug
    {
        public static void Log(string msg)
        {
#if UNITY_EDITOR
            Debug.Log(msg);
#endif
        }
    }
}