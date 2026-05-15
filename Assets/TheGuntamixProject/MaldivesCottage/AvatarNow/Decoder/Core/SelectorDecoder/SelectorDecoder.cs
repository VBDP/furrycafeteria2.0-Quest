
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace AvatarNow.Decoder
{
    //--------------------type---------------------
    public enum SelectorDecoderErrorKind
    {
        None,
        Busy,
        DownloadFailed,
        VersionIsNotMatch,
        InvalidJson,
        Other,
    }

    public class SelectorDecoder : UdonSharpBehaviour
    {
        //--------------------Bunner setting---------------------
        const int BUNNER_WITDH = 512;
        const int BUNNER_HEIGHT = 128;
#if UNITY_ANDROID
        const TextureFormat BUNNER_TEXTURE_FORMAT = TextureFormat.ETC_RGB4Crunched;
#else
        const TextureFormat BUNNER_TEXTURE_FORMAT = TextureFormat.DXT1Crunched;
#endif
        //Decoder is using.
        [HideInInspector]
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; }
        }
        private bool _isBusy;

        //Decoder status for call back
        [HideInInspector]
        public bool Result { get; set; }

        [HideInInspector]
        public SelectorInfo Si
        {
            get { return (SelectorInfo)__selectorInfo; }
            set { __selectorInfo = (object)value; }
        }
        private object __selectorInfo;

        [HideInInspector]
        public SelectorDecoderErrorKind ErrorKind
        {
            get { return (SelectorDecoderErrorKind)_errorKind; }
            set { _errorKind = (object)value; }
        }
        private object _errorKind;

        [HideInInspector]
        public string ErrorMessage { get; set; }

        [HideInInspector]
        public Sprite[] EventBunners { get; set; }

        string _onEndCallBackName;
        string _downloadString;
        string _jsonString;
        int _stringCounter;
        UdonSharpBehaviour _callback;

        public void GetSelectorInfo(VRCUrl url, UdonSharpBehaviour callback, string onEndCallBackName)
        {
            _onEndCallBackName = onEndCallBackName;
            if (_isBusy)
            {
                var errorMessage = "Decoder is Busy";
                DecodeErrorProcess(SelectorDecoderErrorKind.Busy, errorMessage, callback);
            }
            DecoderInitialize();

            _isBusy = true;
            _callback = callback;
            DecoderInitialize();
            StartStopWatch();
            DownLoadStart(url);
        }

        private void DownLoadStart(VRCUrl url)
        {
            //download text
            VRCStringDownloader.LoadUrl(url, (IUdonEventReceiver)this);
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            _downloadString = result.Result;
            EndStopWatch("String download success");
            PreSplitString();
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            var errorMessage = result.ErrorCode.ToString() + ":" + result.Error;
            DecodeErrorProcess(SelectorDecoderErrorKind.DownloadFailed, errorMessage, _callback);
            EndStopWatch("String download Error");
        }


        public void PreSplitString()
        {
            StartProcessTime();
            _stopwatchDeccode.Restart();

            //split texts
            StartStopWatch();
            _stringCounter = 0;
            //check version
            const int ENCODER_VERSION_STRING_LENGTH = 3;
            var encoderVersionString = _downloadString.Substring(_stringCounter, ENCODER_VERSION_STRING_LENGTH);
            _stringCounter += ENCODER_VERSION_STRING_LENGTH;
            var encoderVersion = int.Parse(encoderVersionString);
            SwitchDebug.Log("encoderVersion: [ " + encoderVersion + " ]");
            if (encoderVersion != AvatarNow.DECODER_VERSION)
            {
                var errorMessage = "Data version isn't match.\r\n" +
                    "Encoder is V" + encoderVersion + " , decoder is V" + AvatarNow.DECODER_VERSION + " .\r\n" +
                    "Update to a newer version.";
                DecodeErrorProcess(SelectorDecoderErrorKind.VersionIsNotMatch, errorMessage, _callback);
                return;
            }

            //json string
            const int JSON_LENGTH_STRING_LENGTH = 6;
            var jsonLengthString = _downloadString.Substring(_stringCounter, JSON_LENGTH_STRING_LENGTH);
            _stringCounter += JSON_LENGTH_STRING_LENGTH;
            var jsonLength = int.Parse(jsonLengthString);
            _jsonString = _downloadString.Substring(_stringCounter, jsonLength);
            _stringCounter += jsonLength;
            BranchNextMethodByProcessTime(nameof(JsonDecode));
        }

        public void JsonDecode()
        {
            StartProcessTime();
            StartStopWatch();
            if (VRCJson.TryDeserializeFromJson(_jsonString, out DataToken result))
            {
                EndStopWatch("JsonDecode");
                StartStopWatch();
                var dic = result.DataDictionary;
                Si = SelectorInfo.New(dic);
                EndStopWatch("Create SelectorInfo");
            }
            else
            {
                var errorMessage = "Deserialize information is failed";
                DecodeErrorProcess(SelectorDecoderErrorKind.InvalidJson, errorMessage, _callback);
                return;
            }
            _jsonString = null; //memory clean
            BranchNextMethodByProcessTime(nameof(MakeEventBunnerPrepare));
        }

        /*----------------------------------------CreateTextureBytes-----------------------------------------*/

        int _eventBunnerNumber;
        int _eventBunnerCounter;
        int _eventBunnerOffset;
        public void MakeEventBunnerPrepare()
        {
            _eventBunnerNumber = Si.EventInfos().Length;
            _eventBunnerCounter = 0;
            _eventBunnerOffset = _stringCounter;
            EventBunners = new Sprite[_eventBunnerNumber];
            StartStopWatch();
            MakeOneEventBunner();
        }

        public void MakeOneEventBunner()
        {
            StartProcessTime();
            var bannerDataLength = Si.EventInfos()[_eventBunnerCounter].BannerDataLength();
            var stringStart = _eventBunnerOffset + Si.EventInfos()[_eventBunnerCounter].BannerDataStart();
            var textureString = _downloadString.Substring(stringStart, bannerDataLength);
            _stringCounter += bannerDataLength;
            var textureBytes = Convert.FromBase64String(textureString);

            //make sprite
            var thumbnailTexture = new Texture2D(BUNNER_WITDH, BUNNER_HEIGHT, BUNNER_TEXTURE_FORMAT, true, false); //format is fixed
            thumbnailTexture.LoadRawTextureData(textureBytes);
            thumbnailTexture.Apply();
            var imageWindow = new Rect(0, 0, BUNNER_WITDH, BUNNER_HEIGHT);
            EventBunners[_eventBunnerCounter] = Sprite.Create(thumbnailTexture, imageWindow, Vector2.zero, BUNNER_WITDH, 0, SpriteMeshType.FullRect);
                
            GoNextOneEventBunner();
        }

        private void GoNextOneEventBunner()
        {
            if (++_eventBunnerCounter < _eventBunnerNumber)
            {
                BranchNextMethodByProcessTime(nameof(MakeOneEventBunner));
            }
            else
            {
                //end
                EndStopWatch("MakeTexturesBytes");
                BranchNextMethodByProcessTime(nameof(DecodeComplete));
            }
        }

        public void DecodeComplete()
        {
            MemoryClean();
            _isBusy = false;
            Result = true;
            _callback.SendCustomEvent(_onEndCallBackName);
        }

        private void DecoderInitialize()
        {
            if (EventBunners != null)
            {
                for (int i = 0; i < EventBunners.Length; i++)
                {
                    if (EventBunners[i] != null)
                    {
                        Destroy(EventBunners[i]);
                    }
                }
                EventBunners = null;
            }
            //callback initialize
            Result = false;
            ErrorMessage = "";
            ErrorKind = SelectorDecoderErrorKind.None;
        }

        /*-------------------------Performance tuning----------------------*/
        private const int PROCESS_TIME_MSEC_PER_FRAME = 20;
        private bool _processTimeResetFlag = true;
        private System.Diagnostics.Stopwatch _processTime = new System.Diagnostics.Stopwatch();

        private void StartProcessTime()
        {
            if (_processTimeResetFlag)
            {
                _processTime.Restart();
                _processTimeResetFlag = false;
            }
        }

        private void BranchNextMethodByProcessTime(string nextMethodName)
        {
            if (_processTime.ElapsedMilliseconds < PROCESS_TIME_MSEC_PER_FRAME)
            {
                SendCustomEvent(nextMethodName); //same frame
            }
            else
            {
                SwitchDebug.Log("Next frame");
                SendCustomEventDelayedFrames(nextMethodName, 1); //next frame
                _processTime.Stop();
                _processTimeResetFlag = true;
            }
        }

        private void MemoryClean()
        {
            _downloadString = null;
        }

        /*------------------------------Error------------------------------*/
        private void DecodeErrorProcess(SelectorDecoderErrorKind errorKind, string errorMessage, UdonSharpBehaviour callback)
        {
            if (errorKind != SelectorDecoderErrorKind.Busy)
            {
                _isBusy = false;
            }
            MemoryClean();
            Result = false;
            ErrorKind = errorKind;
            ErrorMessage = errorMessage;
            callback.SendCustomEvent(_onEndCallBackName);
        }

        /*------------------------------for debug------------------------------*/
        private System.Diagnostics.Stopwatch _stopwatchDeccode = new System.Diagnostics.Stopwatch();
        private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
        void StartStopWatch()
        {
            _stopwatch.Restart();
        }
        void EndStopWatch(string methodName)
        {
            _stopwatch.Stop();
            SwitchDebug.Log(methodName + " is done: " + _stopwatch.ElapsedMilliseconds + " [msec]");
        }
    }
}