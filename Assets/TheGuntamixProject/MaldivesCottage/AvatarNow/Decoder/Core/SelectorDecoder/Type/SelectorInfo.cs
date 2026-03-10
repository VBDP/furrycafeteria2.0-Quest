
using Koyashiro.GenericDataContainer;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace AvatarNow.Decoder
{
    [AddComponentMenu("")]
    public class SelectorInfo : UdonSharpBehaviour
    {
        public static SelectorInfo New(EventInfo[] EventInfos)
        {
            var buff = new object[] {
            EventInfos,
            };
            return (SelectorInfo)(object)(buff);
        }

        public static SelectorInfo New(DataDictionary v)
        {
            var vl = v["ei"].DataList;
            var count = vl.Count;
            var eventInfos = DataList<EventInfo>.New();
            for (int i = 0; i < count; i++)
            {
                var data = EventInfo.New(vl[i].DataDictionary);
                if (data.ValidEvent())
                {
#if UNITY_ANDROID
                    if (data.EventTypeName()=="Quest")
                        eventInfos.Add(data);
#else
                    eventInfos.Add(data);
#endif
                }
            }

            return SelectorInfo.New((EventInfo[])eventInfos.ToObjectArray());
        }
    }

    [AddComponentMenu("")]
    public static class SelectorInfoExt
    {
        public static EventInfo[] EventInfos(this SelectorInfo val)
        {
            return (EventInfo[])(((object[])(object)val)[0]);
        }
    }

}

