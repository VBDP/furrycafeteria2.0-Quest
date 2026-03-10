
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace AvatarNow.Decoder
{
    [AddComponentMenu("")]
    public class AvatarInfo : UdonSharpBehaviour
    {
        public static AvatarInfo New(int ThumbnailUrlIndex, int ThumbnailUrlInnerIndex, string AvatarNameJP, string BluePrintID, string CircleNameJP, string AvatarURL, int Index)
        {
            var buff = new object[] {
            ThumbnailUrlIndex,
            ThumbnailUrlInnerIndex,
            AvatarNameJP,
            BluePrintID,
            CircleNameJP,
            AvatarURL,
                Index,
            };
            return (AvatarInfo)(object)(buff);
        }

        public static AvatarInfo New(DataDictionary v, int index)
        {
            var thumbnailUrlIndex = (int)v["ti"].Number;
            var thumbnailUrlInnerIndex = (int)v["tx"].Number;

            var avatarNameJP = v["an"].String;
            var bluePrintID = v["bp"].String;
            var circleNameJP = v["cn"].String;
            var avatarURL = v["au"].String;

            return AvatarInfo.New(thumbnailUrlIndex, thumbnailUrlInnerIndex, avatarNameJP, bluePrintID, circleNameJP, avatarURL, index);
        }
    }

    [AddComponentMenu("")]
    public static class AvatarInfoExt
    {
        public static int ThumbnailUrlIndex(this AvatarInfo val)
        {
            return (int)(((object[])(object)val)[0]);
        }
        public static int ThumbnailUrlInnerIndex(this AvatarInfo val)
        {
            return (int)(((object[])(object)val)[1]);
        }
        public static string AvatarNameJP(this AvatarInfo val)
        {
            return (string)(((object[])(object)val)[2]);
        }
        public static string BluePrintID(this AvatarInfo val)
        {
            return (string)(((object[])(object)val)[3]);
        }
        public static string CircleNameJP(this AvatarInfo val)
        {
            return (string)(((object[])(object)val)[4]);
        }
        public static string AvatarURL(this AvatarInfo val)
        {
            return (string)(((object[])(object)val)[5]);
        }
        public static int Index(this AvatarInfo val)
        {
            return (int)(((object[])(object)val)[6]);
        }
    }

}

