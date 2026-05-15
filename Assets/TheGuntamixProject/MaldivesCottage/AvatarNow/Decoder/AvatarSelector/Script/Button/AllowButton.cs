
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace AvatarNow.Decoder
{
    public class AllowButton : UdonSharpBehaviour
    {
        [SerializeField] private bool isLeft;
        [SerializeField] private bool isFast;
        [SerializeField] private AvatarSelector avatarSelector;
        [SerializeField] private Animator animator;
        private bool _isEnable = true;
        private bool _isVR;
        void Start()
        {
            _isVR = Networking.LocalPlayer.IsUserInVR();
        }

        private void OnEnable()
        {
            animator.SetBool("ENABLE", _isEnable);
            animator.SetBool("PUSH", false);
            animator.SetBool("OVER", false);
        }

        private void OnMouseEnter()
        {
            if (_isVR) return;
            OnOver();
        }

        private void OnMouseExit()
        {
            if (_isVR) return;
            OnExit();
        }

        public void OnOver()
        {
            if (!_isEnable) return;
            animator.SetBool("OVER", true);
        }
        public void OnExit()
        {
            if (!_isEnable) return;
            animator.SetBool("OVER", false);
        }

        public void OnPush()
        {
            if (!_isEnable) return;
            animator.SetBool("PUSH", true);
        }

        public void OnRelease()
        {
            if (!_isEnable) return;
            animator.SetBool("PUSH", false);
        }

        public void OnClick()
        {
            if (!_isEnable) return;
            avatarSelector.OnPushAllowButton(isLeft, isFast);
        }
        public void ButtonEnable(bool enable)
        {
            _isEnable = enable;
            animator.SetBool("ENABLE", _isEnable);
            animator.SetBool("PUSH", false);
            animator.SetBool("OVER", false);
        }
    }
}
