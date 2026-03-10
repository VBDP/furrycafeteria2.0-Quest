using Koyashiro.GenericDataContainer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AvatarNow.Decoder
{
    public enum THUMBNAIL_GET_DESTINATION
    {
        TO_VIEW,
        TO_PAGE,
        TO_PAGE_AND_SELECT,
    }

    public class AvatarSelector : LoadingManegerCallBack
    {
        const int THUMBNAIL_ONE_PAGE_NUM = 9;

        [SerializeField] private AvatarNow avatarNow;
        [SerializeField] private Transform eventButtonParent;
        [SerializeField] private Transform thumbnailButtonParent;
        [SerializeField] private AvatarViewer avatarViewer;
        [SerializeField] private TextMeshProUGUI thumbnailPageText;
        [SerializeField] private GameObject loadingCoverObj;
        [SerializeField] private GameObject uiParentObj;
        [SerializeField] private Image selectIsLockBackGround,selectopen,selectlocked;
        [SerializeField] private AudioControl audioControl;

        private System.Diagnostics.Stopwatch _controlElapseStopWatch = new System.Diagnostics.Stopwatch();
        private LoadingManeger _loadingManeger;
        [HideInInspector]
        private SelectorInfo Si
        {
            get { return (SelectorInfo)_selectorInfo; }
            set { _selectorInfo = (object)value; }
        }
        private object _selectorInfo;

        private const long NON_CONTROL_LIMIT_TIME_MILLISECONDS = 30 * 1000; // 30 sec

        public bool NonControlTimeIsElapsed
        {
            get { return NON_CONTROL_LIMIT_TIME_MILLISECONDS < _controlElapseStopWatch.ElapsedMilliseconds; }
        }

        private AvatarInfo[] _thumbnailAvatars
        {
            get { return (AvatarInfo[])__thumbnailAvatars; }
            set { __thumbnailAvatars = (object)value; }
        }
        private object __thumbnailAvatars;

        private Sprite[] _thumbnailPageSprites = new Sprite[THUMBNAIL_ONE_PAGE_NUM];

        private int _prevSelectEvent = -1;
        private int _viewingEventIndex;
        private int _viewingThumbnailPageIndex = -1; // first page is 1
        private int _viewingThumbnailTotalPageNum = 0;

        private int _readThumbnailCounter;
        private int _selectedEventIndex;
        private int _selectedAvatarIndex;
        private int _eventCount;

        private THUMBNAIL_GET_DESTINATION _thumbnailGetDestination;

        public void OnPushAllowButton(bool isLeft, bool isFast)
        {
            
            if (_viewingThumbnailTotalPageNum <= 1) return; //cant move

            if (!isLeft)
            {
                if (!isFast)
                {
                    //move right
                    if (_viewingThumbnailPageIndex != _viewingThumbnailTotalPageNum)
                    {
                        _viewingThumbnailPageIndex++;
                    }
                }
                else
                {
                    _viewingThumbnailPageIndex = _viewingThumbnailTotalPageNum; //move to last page
                }
            }
            else
            {
                if (!isFast)
                {
                    //move left
                    if (_viewingThumbnailPageIndex > 1)
                    {
                        //first page
                        _viewingThumbnailPageIndex--;
                    }
                }
                else
                {
                    _viewingThumbnailPageIndex = 1; //move to first page
                }
            }
            var firstAvatarIndexInPage = (_viewingThumbnailPageIndex - 1) * THUMBNAIL_ONE_PAGE_NUM;
            //set new thumbnails
            SetThumbnailsFromOneAvatar(_viewingEventIndex, firstAvatarIndexInPage, false);
            Controled();
            audioControl.Select1();
        }

        public void SelectThumnailButtons()
        {
            //button UI
            var thumbnailButtons = thumbnailButtonParent.GetComponentsInChildren<ThumbnailButton>(true);
            var thumbnailCount = thumbnailButtons.Length;

            //set button
            for (int i = 0; i < thumbnailCount; i++)
            {
                var button = thumbnailButtons[i];
                var selected = button.EventIndex == _selectedEventIndex && button.AvatarIndex == _selectedAvatarIndex;
                button.SetSelected(selected);
            }
        }

        public void OnPushEventButton(int eventIndex)
        {
            if (_prevSelectEvent == eventIndex) return;
            _prevSelectEvent = eventIndex;
            _viewingEventIndex = eventIndex;
            //button UI
            CheckMarkEventButton(_viewingEventIndex);

            //SetEventThumbnails
            var firstAvatarIndex = 0; //first
            _thumbnailAvatars = GetThumbnailPageAvatars(_viewingEventIndex, firstAvatarIndex);
            _readThumbnailCounter = 0;
            var urlIndex = _thumbnailAvatars[_readThumbnailCounter].ThumbnailUrlIndex();
            var urlInnerIndex = _thumbnailAvatars[_readThumbnailCounter].ThumbnailUrlInnerIndex();
            _viewingThumbnailTotalPageNum = GetThumbnailTotalPageNum(_viewingEventIndex);
            SetThumbnailPageText(firstAvatarIndex);
            ClearSprites();
            loadingCoverObj.SetActive(true);
            thumbnailButtonParent.gameObject.SetActive(false);
            StartGetThumbnail(urlIndex, urlInnerIndex, THUMBNAIL_GET_DESTINATION.TO_PAGE);
            Controled();
            audioControl.Select1();
        }

        public void SelectIdLockUI(bool selectIsLock)
        {
            selectIsLockBackGround.color = selectIsLock ? Color.red : Color.white;
            selectlocked.enabled = selectIsLock ? true : false;
            selectopen.enabled = selectIsLock ? false : true;
            Controled();
        }

        public void ViewUI(bool viewEnable)
        {
            uiParentObj.SetActive(viewEnable);
        }

        private void StartGetThumbnail(int urlIndex, int urlInnerIndex, THUMBNAIL_GET_DESTINATION destination)
        {
            _thumbnailGetDestination = destination;
            _loadingManeger.TryGetThumbnailStart(urlIndex, urlInnerIndex, this);
        }

        private bool _initialized = false;
        private void CompleteGetThumbnail()
        {
            if (!_initialized)
            {
                avatarViewer.SetLoadingCover(false);
                _initialized = true;
            }
            thumbnailButtonParent.gameObject.SetActive(true);
            loadingCoverObj.SetActive(false);
            avatarNow.Initialized = true;
        }

        public void OnPushThumbnailButton(int eventIndex, int avatarIndex)
        {
            if (_selectedEventIndex == eventIndex && _selectedAvatarIndex == avatarIndex) return; //same button pushed
            _selectedEventIndex = eventIndex;
            _selectedAvatarIndex = avatarIndex;
            SelectThumbnail(_selectedEventIndex, _selectedAvatarIndex);
            Controled();
        }


        public void SelectThumbnail(int eventIndex, int avatarIndex)
        {
            var info = Si.EventInfos()[eventIndex].AvatarInfos()[avatarIndex];
            StartGetThumbnail(info.ThumbnailUrlIndex(), info.ThumbnailUrlInnerIndex(), THUMBNAIL_GET_DESTINATION.TO_VIEW);
            SelectThumnailButtons();
        }

        public void InitAvatarSelector(LoadingManeger loadingManeger, SelectorInfo selectorInfo, int selectedEventIndex, Sprite[] bunnerSprites)
        {
            _loadingManeger = loadingManeger;
            Si = selectorInfo;
            SetEventButtons(selectedEventIndex, bunnerSprites);
            Controled();
        }

        public void SetOneThumbnailAndSelect(int eventIndex, int avatarIndex)
        {
            _selectedEventIndex = eventIndex;
            _selectedAvatarIndex = avatarIndex;
            var info = Si.EventInfos()[_selectedEventIndex].AvatarInfos()[_selectedAvatarIndex];
            StartGetThumbnail(info.ThumbnailUrlIndex(), info.ThumbnailUrlInnerIndex(), THUMBNAIL_GET_DESTINATION.TO_VIEW);
        }

        public void SetThumbnailsFromOneAvatar(int eventIndex, int avatarIndex, bool select)
        {
            if (select)
            {
                _selectedEventIndex = eventIndex;
                _selectedAvatarIndex = avatarIndex;
            }
            _viewingEventIndex = eventIndex;
            CheckMarkEventButton(_viewingEventIndex);
            _thumbnailAvatars = GetThumbnailPageAvatars(eventIndex, avatarIndex);
            _readThumbnailCounter = 0;
            var urlIndex = _thumbnailAvatars[_readThumbnailCounter].ThumbnailUrlIndex();
            var urlInnerIndex = _thumbnailAvatars[_readThumbnailCounter].ThumbnailUrlInnerIndex();
            var destination = select ? THUMBNAIL_GET_DESTINATION.TO_PAGE_AND_SELECT : THUMBNAIL_GET_DESTINATION.TO_PAGE;
            _viewingThumbnailTotalPageNum = GetThumbnailTotalPageNum(_viewingEventIndex);
            SetThumbnailPageText(avatarIndex);
            ClearSprites();
            loadingCoverObj.SetActive(true);
            thumbnailButtonParent.gameObject.SetActive(false);
            StartGetThumbnail(urlIndex, urlInnerIndex, destination);
        }
        private void SetThumbnailPageText(int avatarIndex)
        {
            _viewingThumbnailPageIndex = avatarIndex / THUMBNAIL_ONE_PAGE_NUM + 1;
            thumbnailPageText.text = _viewingThumbnailPageIndex + " / " + _viewingThumbnailTotalPageNum;
        }

        private int GetThumbnailTotalPageNum(int eventIndex)
        {
            var avatarTotalNum = Si.EventInfos()[eventIndex].AvatarInfos().Length;
            if (avatarTotalNum == 0) return 0;
            var pageNum = avatarTotalNum / THUMBNAIL_ONE_PAGE_NUM;
            if ((avatarTotalNum % THUMBNAIL_ONE_PAGE_NUM) != 0) pageNum++;
            return pageNum;
        }

        private void ClearSprites()
        {
            if (_thumbnailPageSprites != null)
            {
                foreach (var sprite in _thumbnailPageSprites)
                {
                    if (sprite != null)
                    {
                        Destroy(sprite);
                    }
                }
            }
        }

        public override void OnThumbnaiDecodeEnd(bool result, Sprite thumbnail, ThumbnailDecoderErrorKind errorKind, string errorMessage)
        {
            if (result)
            {
                switch (_thumbnailGetDestination)
                {
                    case THUMBNAIL_GET_DESTINATION.TO_PAGE:
                    case THUMBNAIL_GET_DESTINATION.TO_PAGE_AND_SELECT:
                        _thumbnailPageSprites[_readThumbnailCounter] = thumbnail;
                        if (++_readThumbnailCounter < _thumbnailAvatars.Length)
                        {
                            var urlIndex = _thumbnailAvatars[_readThumbnailCounter].ThumbnailUrlIndex();
                            var urlInnerIndex = _thumbnailAvatars[_readThumbnailCounter].ThumbnailUrlInnerIndex();
                            _loadingManeger.TryGetThumbnailStart(urlIndex, urlInnerIndex, this);
                        }
                        else
                        {
                            //read finish
                            SetReadThumbnailButtons();
                            if (_thumbnailGetDestination == THUMBNAIL_GET_DESTINATION.TO_PAGE_AND_SELECT)
                            {
                                SelectThumbnail(_selectedEventIndex, _selectedAvatarIndex);
                            }
                            CompleteGetThumbnail();
                        }
                        break;
                    case THUMBNAIL_GET_DESTINATION.TO_VIEW:
                        var selectedAvatarInfo = Si.EventInfos()[_selectedEventIndex].AvatarInfos()[_selectedAvatarIndex];
                        avatarViewer.SetAvatarView(selectedAvatarInfo, _selectedEventIndex, _selectedAvatarIndex, thumbnail);
                        CompleteGetThumbnail();
                        break;
                    default: break;

                }
            }
        }

        private void SetReadThumbnailButtons()
        {
            //button UI
            var thumbnailButtons = thumbnailButtonParent.GetComponentsInChildren<ThumbnailButton>(true);
            var avatarCount = _thumbnailAvatars.Length;
            //active
            for (int i = 0; i < thumbnailButtons.Length; i++)
            {
                thumbnailButtons[i].gameObject.SetActive(i < avatarCount);
            }

            //set button
            for (int i = 0; i < avatarCount; i++)
            {
                var info = _thumbnailAvatars[i];
                var sprite = _thumbnailPageSprites[i];
                thumbnailButtons[i].SetThumbnailButton(sprite, info.AvatarNameJP(), info.CircleNameJP(), _viewingEventIndex, info.Index());
            }
            SelectThumnailButtons();
        }

        public AvatarInfo[] GetThumbnailPageAvatars(int eventIndex, int avatarIndex)
        {
            var avatarInfos = Si.EventInfos()[eventIndex].AvatarInfos();

            var page = avatarIndex / THUMBNAIL_ONE_PAGE_NUM;
            var start = page * THUMBNAIL_ONE_PAGE_NUM;
            var end = start + THUMBNAIL_ONE_PAGE_NUM - 1;
            var avatarTotalCount = avatarInfos.Length;
            end = end < avatarTotalCount ? end : avatarTotalCount - 1;
            var avatarCount = end - start + 1;
            var avatarCounter = start;

            var avatars = DataList<AvatarInfo>.New();
            for (int i = 0; i < avatarCount; i++)
            {
                avatars.Add(avatarInfos[avatarCounter++]);
            }

            return (AvatarInfo[])avatars.ToObjectArray();
        }


        public void SetEventButtons(int selectedEventIndex, Sprite[] bunnerSprites)
        {
            var eventButtons = eventButtonParent.GetComponentsInChildren<EventButton>(true);
            var buttonCount = eventButtons.Length;
            _eventCount = bunnerSprites.Length;
            for (int i = 0; i < buttonCount; i++)
            {
                var eventButton = eventButtons[i];
                var buttonSiblingIndex = eventButton.gameObject.transform.GetSiblingIndex();
                var existEvent = buttonSiblingIndex < _eventCount;
                eventButton.gameObject.SetActive(existEvent);
                if (!existEvent)
                {
                    continue;
                }

                var eventIndex = (_eventCount - 1) - buttonSiblingIndex; //reverse position
                eventButton.SetEventButton(eventIndex, bunnerSprites[eventIndex]);
                eventButtons[i].SetCheckMark(selectedEventIndex);
            }
        }

        public void CheckMarkEventButton(int selectedEventIndex)
        {
            var eventButtons = eventButtonParent.GetComponentsInChildren<EventButton>(false);
            var buttonCount = eventButtons.Length;
            for (int i = 0; i < buttonCount; i++)
            {
                eventButtons[i].SetCheckMark(selectedEventIndex);
            }
        }

        private void Controled()
        {
            _controlElapseStopWatch.Restart();
        }

        public override void OnSelectorDecodeEnd(bool result, SelectorInfo selectorInfo, Sprite[] EventBunners, SelectorDecoderErrorKind errorKind, string errorMessage)
        {
            //not use on this script.
        }

    }
}