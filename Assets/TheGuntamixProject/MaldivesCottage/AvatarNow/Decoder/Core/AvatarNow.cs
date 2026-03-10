#define AVATAR_NOW_ENABLE_SELECTOR

using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace AvatarNow.Decoder
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class AvatarNow : LoadingManegerCallBack
    {
        public const int DECODER_VERSION = 1;

        [SerializeField] private LoadingManeger loadingManeger;
        [SerializeField] private AvatarViewer avatarViewer;
        [SerializeField] private GameObject avatarSelectorObj;
        [SerializeField] private GameObject avatarSelectorCloseButton;

#if AVATAR_NOW_ENABLE_SELECTOR
        private AvatarSelector _avatarSelector;
#endif

        private int _selectedEventIndex;
        private int _selectedAvatarIndex;
        private bool _selectIsLocked = false;
        const int RANDOM_SELECT_INTERVAL_SECONDS = 600; //3600

        private bool _shareQueueFlag = false;

        private int _timeCheckCounter = 0;
        const int TIME_CHECK_INTERVAL_FRAME = 60;
        private int _prevIntervalCoeff = 0;

        private SelectorInfo _selectorInfo
        {
            get { return (SelectorInfo)__selectorInfo; }
            set { __selectorInfo = (object)value; }
        }
        private object __selectorInfo;
        private bool _initialSyncWaited = false;

        public bool Initialized
        {
            get { return _initialized; }
            set
            {
                _initialized = value;
            }
        }
        private bool _initialized = false;

        void Start()
        {
            if (!avatarSelectorObj.activeSelf)
                avatarSelectorCloseButton.SetActive(false);
#if AVATAR_NOW_ENABLE_SELECTOR
            _avatarSelector = avatarSelectorObj.GetComponent<AvatarSelector>();
            _avatarSelector.ViewUI(false); //disable view until prepare thumbnails
#endif
            avatarViewer.SetLoadingCover(true);
            loadingManeger.TrySelectorDecodeStart(this);
            _prevIntervalCoeff = GetIntervalCoeff();
            SendCustomEventDelayedSeconds(nameof(ChangeSyncValue), 10.0f); //value is sync after 10sec. You can change sync value.
        }

        public override void OnSelectorDecodeEnd(bool result, SelectorInfo selectorInfo, Sprite[] eventBunners, SelectorDecoderErrorKind errorKind, string errorMessage)
        {
            if (result)
            {
                _selectorInfo = selectorInfo;
                if (!_reloadSelectorFlag)
                {
                    GetRandomAvatar(_selectorInfo, out _selectedEventIndex, out _selectedAvatarIndex);
                    Debug.Log("selectedEventIndex:" + _selectedEventIndex);
                    Debug.Log("selectedAvatarIndex:" + _selectedAvatarIndex);
                }
                _reloadSelectorFlag = false;

#if AVATAR_NOW_ENABLE_SELECTOR
                //valid selector mode.
                _avatarSelector.InitAvatarSelector(loadingManeger, _selectorInfo, _selectedEventIndex, eventBunners);
                _avatarSelector.gameObject.SetActive(true);
                //select button
                _avatarSelector.SetThumbnailsFromOneAvatar(_selectedEventIndex, _selectedAvatarIndex, true);
#else
                //invalid selector mode.
                StartGetThumbnailAndSetView();
#endif
            }
            else
            {
                Debug.LogWarning(errorMessage);
            }
        }



        private void GetRandomAvatar(SelectorInfo selectorInfo, out int eventIndex, out int avatarIndex)
        {
            for (int i = 0; i < selectorInfo.EventInfos().Length; i++)
            {
                var eventInfo = selectorInfo.EventInfos()[i];
                if (eventInfo.ActiveEvent())
                {
                    eventIndex = i;
                    //get random
                    var nowIntervalCoeff = GetIntervalCoeff();
                    UnityEngine.Random.InitState(nowIntervalCoeff);
                    var avatarCount = eventInfo.AvatarInfos().Length;
                    int rnd = UnityEngine.Random.Range(0, avatarCount); //by specific . int random max number is count -1.
                    rnd = rnd < avatarCount ? rnd : avatarCount - 1; //just in case. limit.
                    avatarIndex = rnd;
                    return;
                }
            }
            eventIndex = -1;
            avatarIndex = -1;
        }

        public void OnPushSyncButton()
        {
            var syncSharedAvatarIndex = (avatarViewer.selectedEventIndex * 1000) + avatarViewer.selectedAvatarIndex; //merge sync val
            SetSyncSharedIndex(syncSharedAvatarIndex);
        }


#if AVATAR_NOW_ENABLE_SELECTOR
        public void OnPushSelectLockButton()
        {
            _selectIsLocked = !_selectIsLocked;
            _avatarSelector.SelectIdLockUI(_selectIsLocked);
        }
#endif

        [UdonSynced, FieldChangeCallback(nameof(SyncSharedAvatarIndex))] private int _syncSharedIndex = -1;
        public int SyncSharedAvatarIndex
        {
            get => _syncSharedIndex;
            set
            {
                _syncSharedIndex = value;
                ApplySyncSharedIndex();
                Debug.Log("SyncSharedAvatarIndex is:" + _syncSharedIndex);
            }
        }

        public void ChangeSyncValue()
        {
            _initialSyncWaited = true;
        }

        private bool _reloadSelectorFlag = false;

        public void ApplySyncSharedIndex()
        {
            if (!_initialSyncWaited || _selectIsLocked) return;
            var eventIndex = _syncSharedIndex / 1000;
            var avatarIndex = _syncSharedIndex % 1000;
            _selectedEventIndex = eventIndex;
            _selectedAvatarIndex = avatarIndex;
            _shareQueueFlag = true;
            var eventCount = _selectorInfo.EventInfos().Length;
            if (_selectedEventIndex >= eventCount) //index range over
            {
                //reload
                _reloadSelectorFlag = true;
                return;
            }
            var avatarCount = _selectorInfo.EventInfos()[eventIndex].AvatarInfos().Length;
            if (_selectedAvatarIndex >= avatarCount) //index range over
            {
                //reload
                _reloadSelectorFlag = true;
                return;
            }
        }

        private void SetSyncSharedIndex(int value)
        {
            if (_syncSharedIndex != value)
            {
                TakeOwner();
                _syncSharedIndex = value;
                RequestSerialization();
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ApplySyncSharedIndex));
            }
        }

        private void Update()
        {
            //share button sync
            if (_initialized && _initialSyncWaited && _shareQueueFlag && !_selectIsLocked && !loadingManeger.IsBusy)
            {
                _shareQueueFlag = false;
                if (_reloadSelectorFlag)
                {
                    loadingManeger.TrySelectorDecodeStart(this);
                    return;
                }
#if AVATAR_NOW_ENABLE_SELECTOR
                _avatarSelector.SetOneThumbnailAndSelect(_selectedEventIndex, _selectedAvatarIndex);
#else
                //invalid selector mode.
                StartGetThumbnailAndSetView();
#endif
            }

            //time interval select
            if (_initialized && _initialSyncWaited && !_shareQueueFlag && !_selectIsLocked && !loadingManeger.IsBusy && (++_timeCheckCounter % TIME_CHECK_INTERVAL_FRAME) == 0)
            {
                var nowIntervalCoeff = GetIntervalCoeff();
                if (_prevIntervalCoeff != nowIntervalCoeff)
                {
                    _prevIntervalCoeff = nowIntervalCoeff;
                    GetRandomAvatar(_selectorInfo, out _selectedEventIndex, out _selectedAvatarIndex);
#if AVATAR_NOW_ENABLE_SELECTOR
                    //select button
                    if (_avatarSelector.NonControlTimeIsElapsed)
                    {
                        _avatarSelector.SetOneThumbnailAndSelect(_selectedEventIndex, _selectedAvatarIndex);
                    }
#else
                    StartGetThumbnailAndSetView();
#endif
                }
            }
        }

        public override void OnThumbnaiDecodeEnd(bool result, Sprite thumbnail, ThumbnailDecoderErrorKind errorKind, string errorMessage)
        {
            if (result)
            {
                avatarViewer.SetLoadingCover(false);
                var selectedAvatarInfo = _selectorInfo.EventInfos()[_selectedEventIndex].AvatarInfos()[_selectedAvatarIndex];
                avatarViewer.SetAvatarView(selectedAvatarInfo, _selectedEventIndex, _selectedAvatarIndex, thumbnail);
                _initialized = true;
            }
            else
            {
                Debug.LogWarning(errorMessage);
            }
        }

        private void StartGetThumbnailAndSetView()
        {
            var selectedAvatarInfo = _selectorInfo.EventInfos()[_selectedEventIndex].AvatarInfos()[_selectedAvatarIndex];
            var urlIndex = selectedAvatarInfo.ThumbnailUrlIndex();
            var urlInnerIndex = selectedAvatarInfo.ThumbnailUrlInnerIndex();
            loadingManeger.TryGetThumbnailStart(urlIndex, urlInnerIndex, this);
        }

        /*----------------------------util--------------------------------------------*/
        private void TakeOwner()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }

        private int GetIntervalCoeff()
        {
            var nowUnixTimeInt = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            return (int)(nowUnixTimeInt / RANDOM_SELECT_INTERVAL_SECONDS); //change per interval
        }
    }
}
