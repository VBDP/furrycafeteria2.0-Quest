using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace AvatarNow.Decoder
{
    public class EventButton : UdonSharpBehaviour
    {
        public Image bunner;
        public GameObject checkMark;
        public AvatarSelector avatarSelector;
        private int _eventIndex;

        public void SetEventButton(int eventIndex, Sprite sp)
        {
            _eventIndex = eventIndex;
            bunner.sprite = sp;
        }

        public void SetCheckMark(int setIndex)
        {

            checkMark.SetActive(setIndex == _eventIndex);
        }

        public void OnPush()
        {
            avatarSelector.OnPushEventButton(_eventIndex);
        }

    }
}
