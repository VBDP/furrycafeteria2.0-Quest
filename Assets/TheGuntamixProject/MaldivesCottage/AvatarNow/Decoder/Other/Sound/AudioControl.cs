using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace AvatarNow.Decoder
{
    public class AudioControl : UdonSharpBehaviour
    {
        [SerializeField] private AudioSource audiosurce;
        [SerializeField] private AudioClip[] clip;

        public void Open()
        {
                audiosurce.PlayOneShot(clip[0]);
        }
        public void Close()
        {
                audiosurce.PlayOneShot(clip[1]);
        }
        public void Select1()
        {
                audiosurce.PlayOneShot(clip[2]);
        }
        public void Select2()
        {
                audiosurce.PlayOneShot(clip[3]);
        }
        public void Select3()
        {
            audiosurce.PlayOneShot(clip[4]);
        }
    }
}