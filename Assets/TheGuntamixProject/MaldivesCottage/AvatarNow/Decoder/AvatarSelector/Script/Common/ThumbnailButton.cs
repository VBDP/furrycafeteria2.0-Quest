
using AvatarNow.Decoder;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

public class ThumbnailButton : UdonSharpBehaviour
{
    [SerializeField] private AvatarSelector avatarSelector;
    [SerializeField] private AudioControl audioControl;
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private GameObject disableCover;
    [SerializeField] private TextMeshProUGUI avatarNameText;
    [SerializeField] private TextMeshProUGUI creatorNameText;
    [SerializeField] private Animator animator;

    [HideInInspector]
    public int EventIndex
    {
        get { return _eventIndex; }
    }
    private int _eventIndex;

    [HideInInspector]
    public int AvatarIndex
    {
        get { return _avatarIndex; }
    }
    private int _avatarIndex;
    private bool _isVR;
    private bool _isEnable;
    private bool _isSelected;

    void Start()
    {
        animator.SetBool("RESET", true);
        _isVR = Networking.LocalPlayer.IsUserInVR();
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        animator.SetBool("SELECTED", _isSelected);
        animator.SetBool("RESET", true);
    }

    public void SetThumbnailButton(Sprite thumbnail, string avatarName, string circleName, int eventIndex, int avatarIndex)
    {
        thumbnailImage.sprite = thumbnail;
        avatarNameText.text = avatarName;
        creatorNameText.text = circleName;
        _eventIndex = eventIndex;
        _avatarIndex = avatarIndex;
    }

    private void OnEnable()
    {
        animator.SetBool("RESET", true);
        animator.SetBool("SELECTED", _isSelected);
        animator.SetBool("PUSH", false);
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
        animator.SetBool("RESET", false);
        animator.SetBool("OVER", true);
    }
    public void OnExit()
    {
        animator.SetBool("RESET", false);
        animator.SetBool("OVER", false);
    }

    public void OnPush()
    {
        if (!_isEnable) return;
        animator.SetBool("RESET", false);
        animator.SetBool("PUSH", true);
        
    }

    public void OnRelease()
    {
        animator.SetBool("RESET", false);
        animator.SetBool("PUSH", false);
    }

    public void ButtonEnable(bool enable)
    {
        animator.SetBool("RESET", true);
        _isEnable = enable;
        disableCover.SetActive(!enable);
    }

    public void OnClick()
    {
        avatarSelector.OnPushThumbnailButton(_eventIndex, _avatarIndex);
        audioControl.Select2();
    }
}
