using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace AvatarNow.Decoder
{
    [AddComponentMenu("")]
    public class ThumbnailData : UdonSharpBehaviour
    {
        public static ThumbnailData New(int StartIndex, int DataLength)
        {
            var buff = new object[] {
            StartIndex,
            DataLength,
            };
            return (ThumbnailData)(object)(buff);
        }

        public static ThumbnailData New(DataDictionary v)
        {
            var startIndex = (int)v["si"].Number;
            var dataLength = (int)v["dl"].Number;
            return ThumbnailData.New(startIndex, dataLength);
        }
    }

    [AddComponentMenu("")]
    public static class ThumbnailDataExt
    {
        public static int StartIndex(this ThumbnailData val)
        {
            return (int)(((object[])(object)val)[0]);
        }
        public static int DataLength(this ThumbnailData val)
        {
            return (int)(((object[])(object)val)[1]);
        }
    }
}