
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace AvatarNow.Decoder
{
    public class AvatarViewer : UdonSharpBehaviour
    {
        [SerializeField] private Image thumbnail;
        [SerializeField] private TextMeshProUGUI circleNameText;
        [SerializeField] private TextMeshProUGUI avatarNameText;
        [SerializeField] private InputField urlInputField;
        [SerializeField] private VRC_AvatarPedestal avatarPedestal;
        [SerializeField] private GameObject loadingCover;

        public int selectedEventIndex = -1;
        public int selectedAvatarIndex = -1;

        private Sprite _nowSprite;

        public void SetAvatarView(AvatarInfo avatarInfo, int selectedEventIndex, int selectedAvatarIndex, Sprite sprite)
        {
            this.selectedEventIndex = selectedEventIndex;
            this.selectedAvatarIndex = selectedAvatarIndex;

            //memory clear
            if (_nowSprite != null)
            {
                Destroy(_nowSprite);
            }
            //new sprite
            _nowSprite = sprite;

            //thumbnail
            thumbnail.sprite = _nowSprite;

            //circleName
            var circleName = avatarInfo.CircleNameJP();
            circleNameText.gameObject.SetActive(!circleName.Equals(""));
            circleNameText.text = circleName;

            //avatarName
            var avatarName = avatarInfo.AvatarNameJP();
            avatarNameText.gameObject.SetActive(!avatarName.Equals(""));
            avatarNameText.text = avatarName;

            //url
            var url = avatarInfo.AvatarURL();
            urlInputField.gameObject.SetActive(!url.Equals(""));
            urlInputField.text = url;

            //pedestal
            var blueprintID = avatarInfo.BluePrintID();
            var validBlueprintID = !blueprintID.Equals("");
            avatarPedestal.gameObject.SetActive(validBlueprintID);
            if (validBlueprintID)
            {
                avatarPedestal.SwitchAvatar(blueprintID);
            }
        }

        public void SetLoadingCover(bool coverEnable)
        {
            loadingCover.SetActive(coverEnable);
        }
    }
}