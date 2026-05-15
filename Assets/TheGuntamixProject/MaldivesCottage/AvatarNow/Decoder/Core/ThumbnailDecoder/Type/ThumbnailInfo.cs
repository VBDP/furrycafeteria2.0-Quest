using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace AvatarNow.Decoder
{
    [AddComponentMenu("")]
    public class ThumbnailInfo : UdonSharpBehaviour
    {
        public static ThumbnailInfo New(ThumbnailData[] ThumbnailData)
        {
            var buff = new object[] {
            ThumbnailData,
            };
            return (ThumbnailInfo)(object)(buff);
        }

        public static ThumbnailInfo New(DataDictionary v)
        {
            var vl = v["td"].DataList;
            var count = vl.Count;
            var thumbnailData = new object[count];
            for (int i = 0; i < count; i++)
            {
                thumbnailData[i] = ThumbnailData.New(vl[i].DataDictionary);
            }

            return ThumbnailInfo.New((ThumbnailData[])thumbnailData);
        }
    }

    [AddComponentMenu("")]
    public static class ThumbnailInfoExt
    {
        public static ThumbnailData[] ThumbnailData(this ThumbnailInfo val)
        {
            return (ThumbnailData[])(((object[])(object)val)[0]);
        }
    }
}