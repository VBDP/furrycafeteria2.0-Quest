
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;


namespace AvatarNow.Decoder
{
    [AddComponentMenu("")]
    public class EventInfo : UdonSharpBehaviour
    {
        public static EventInfo New(string EventName, string EventTypeName, bool ActiveEvent, bool ValidEvent, int BannerDataStart, int BannerDataLength, AvatarInfo[] AvatarInfos)
        {
            var buff = new object[] {
            EventName,
            EventTypeName,
            ActiveEvent,
            ValidEvent,
            BannerDataStart,
            BannerDataLength,
            AvatarInfos,
            };
            return (EventInfo)(object)(buff);
        }

        public static EventInfo New(DataDictionary v)
        {

            var eventName = v["en"].String;
            var eventTypeName = v["et"].String;
            var activeEvent = v["ae"].Boolean;
            var validEvent = v["ve"].Boolean;
            var bannerDataStart = (int)v["bs"].Number;
            var bannerDataLength = (int)v["bl"].Number;

            var vl = v["ai"].DataList;
            var count = vl.Count;
            var avatarInfos = new object[count];
            for (int i = 0; i < count; i++)
            {
                avatarInfos[i] = AvatarInfo.New(vl[i].DataDictionary, i);
            }

            return EventInfo.New(eventName, eventTypeName, activeEvent, validEvent, bannerDataStart, bannerDataLength, (AvatarInfo[])avatarInfos);
        }
    }

    [AddComponentMenu("")]
    public static class EventInfoInfoExt
    {
        public static string EventName(this EventInfo val)
        {
            return (string)(((object[])(object)val)[0]);
        }
        public static string EventTypeName(this EventInfo val)
        {
            return (string)(((object[])(object)val)[1]);
        }
        public static bool ActiveEvent(this EventInfo val)
        {
            return (bool)(((object[])(object)val)[2]);
        }
        public static bool ValidEvent(this EventInfo val)
        {
            return (bool)(((object[])(object)val)[3]);
        }
        public static int BannerDataStart(this EventInfo val)
        {
            return (int)(((object[])(object)val)[4]);
        }
        public static int BannerDataLength(this EventInfo val)
        {
            return (int)(((object[])(object)val)[5]);
        }
        public static AvatarInfo[] AvatarInfos(this EventInfo val)
        {
            return (AvatarInfo[])(((object[])(object)val)[6]);
        }
    }

}


