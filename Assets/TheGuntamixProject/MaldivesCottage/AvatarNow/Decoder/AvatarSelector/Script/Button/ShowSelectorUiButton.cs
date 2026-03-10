
using UdonSharp;
using UnityEngine;

namespace AvatarNow.Decoder
{
    public class ShowSelectorUiButton : UdonSharpBehaviour
    {
        [SerializeField] private Animator avatarSelectorAnimator;
        [SerializeField] private string animationname;
        [SerializeField] private float animationspeed;
        [SerializeField] private float startframe;

        public void OnPush()
        {
            //toggle
            avatarSelectorAnimator.SetFloat(Animator.StringToHash("Speed"),animationspeed);
            avatarSelectorAnimator.Play(animationname, 0, startframe);
        }
    }
}