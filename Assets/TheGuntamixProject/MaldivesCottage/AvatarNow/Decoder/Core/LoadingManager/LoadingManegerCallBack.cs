
using UdonSharp;
using UnityEngine;

namespace AvatarNow.Decoder
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public abstract class LoadingManegerCallBack : UdonSharpBehaviour
    {
        //SelectorDecoder
        abstract public void OnSelectorDecodeEnd(bool result, SelectorInfo selectorInfo, Sprite[] EventBunners, SelectorDecoderErrorKind errorKind, string errorMessage);

        //ThumbnailDecoder
        abstract public void OnThumbnaiDecodeEnd(bool result, Sprite thumbnail, ThumbnailDecoderErrorKind errorKind, string errorMessage);
    }
}