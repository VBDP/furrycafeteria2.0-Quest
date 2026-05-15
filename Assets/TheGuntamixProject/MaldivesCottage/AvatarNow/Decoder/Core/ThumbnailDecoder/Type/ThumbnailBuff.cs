using UdonSharp;
using UnityEngine;

namespace AvatarNow.Decoder
{
    [AddComponentMenu("")]
    public class ThumbnailBuff : UdonSharpBehaviour
    {
        public static ThumbnailBuff New(int UrlIndex, byte[][] ThumbnailBytes)
        {
            var buff = new object[] {
            UrlIndex,
            ThumbnailBytes,
            };
            return (ThumbnailBuff)(object)(buff);
        }
    }

    [AddComponentMenu("")]
    public static class ThumbnailBuffExt
    {
        public static int UrlIndex(this ThumbnailBuff val)
        {
            return (int)(((object[])(object)val)[0]);
        }
        public static byte[][] ThumbnailBytes(this ThumbnailBuff val)
        {
            return (byte[][])(((object[])(object)val)[1]);
        }
    }
}