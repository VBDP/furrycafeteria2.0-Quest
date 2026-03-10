
using Koyashiro.GenericDataContainer;
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
    public enum ThumbnailDecoderErrorKind
    {
        None,
        Busy,
        DownloadFailed,
        VersionIsNotMatch,
        InvalidJson,
        Other,
    }

    public class ThumbnailDecoder : UdonSharpBehaviour
    {

        //--------------------Thumbnail setting---------------------
        const int THUMBNAIL_WITDH = 1024;
        const int THUMBNAIL_HEIGHT = 1024;
#if UNITY_ANDROID
        const TextureFormat THUMBNAIL_TEXTURE_FORMAT = TextureFormat.ETC_RGB4Crunched;
#else
        const TextureFormat THUMBNAIL_TEXTURE_FORMAT = TextureFormat.DXT1Crunched;
#endif
        /*----------------------------------------CreateTextureBytes-----------------------------------------*/
        byte[][] _texturesBytes;
        const int ONE_DISPLAY_THUMBNAIL_NUM = 9;
        const int THUMBNAIL_BUFF_NUM = 3;

        /*----------------------------------------DownLoadThumbnailData-----------------------------------------*/
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
        public ThumbnailDecoderErrorKind ErrorKind
        {
            get { return (ThumbnailDecoderErrorKind)_errorKind; }
            set { _errorKind = (object)value; }
        }
        private object _errorKind;

        [HideInInspector]
        public string ErrorMessage { get; set; }

        //thumbnailDataBuff
        private DataList<ThumbnailBuff> _thumbnailBuff
        {
            get { return (DataList<ThumbnailBuff>)___thumbnailBuff; }
            set { ___thumbnailBuff = (object)value; }
        }
        private object ___thumbnailBuff;

        private ThumbnailInfo _thumbnailInfo
        {
            get { return (ThumbnailInfo)__thumbnailInfo; }
            set { __thumbnailInfo = (object)value; }
        }
        private object __thumbnailInfo;

        [HideInInspector]
        public Sprite Thumbnail;

        //--------------------private---------------------
        private string _downloadString;
        private string _jsonString;

        private int _base64DataOffset = 0;

        // callback when progressed
        private UdonSharpBehaviour _callback;
        private string _onEndCallBackName;

        private int _targetInnerIndex;
        private int _targetUrlIndex;

        private void Start()
        {
            _thumbnailBuff = DataList<ThumbnailBuff>.New();
        }

        /// <summary>
        /// Get thumnail.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public void GetThumbnail(int urlIndex, int innerIndex, VRCUrl url, UdonSharpBehaviour callback, string onEndCallBackName)
        {
            //check thumbnail in buffer
            var thumbnailBytes = GetThumbnailBytes(urlIndex);
            if (thumbnailBytes != null)
            {
                //hit buffer cashe
                Debug.Log("hit: " + urlIndex + " / " + innerIndex);
                Thumbnail = GetSpriteFromtextureBytes(thumbnailBytes[innerIndex]);
                DecodeComplete();
                return;
            }

            Debug.Log("unhit: " + urlIndex + " / " + innerIndex);
            //no hit cashe. load new thumbnailData
            _onEndCallBackName = onEndCallBackName;
            if (_isBusy)
            {
                var errorMessage = "Decoder is Busy";
                DecodeErrorProcess(ThumbnailDecoderErrorKind.Busy, errorMessage, callback);
            }
            DecoderInitialize();

            _targetUrlIndex = urlIndex;
            _targetInnerIndex = innerIndex;
            _isBusy = true;
            _callback = callback;
            DecoderInitialize();
            StartStopWatch();
            DownLoadStart(url);
        }

        private byte[][] GetThumbnailBytes(int urlIndex)
        {
            var count = _thumbnailBuff.Count();
            for (int i = 0; i < count; i++)
            {
                if (_thumbnailBuff.GetValue(i).UrlIndex() == urlIndex)
                {
                    return _thumbnailBuff.GetValue(i).ThumbnailBytes();
                }
            }
            return null; //not find.
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
            DecodeErrorProcess(ThumbnailDecoderErrorKind.DownloadFailed, errorMessage, _callback);
            EndStopWatch("String download Error");
        }


        public void PreSplitString()
        {
            StartProcessTime();
            _stopwatchDeccode.Restart();

            //split texts
            StartStopWatch();
            var stringCounter = 0;

            //check version
            const int ENCODER_VERSION_STRING_LENGTH = 3;
            var encoderVersionString = _downloadString.Substring(stringCounter, ENCODER_VERSION_STRING_LENGTH);
            stringCounter += ENCODER_VERSION_STRING_LENGTH;
            var encoderVersion = int.Parse(encoderVersionString);
            SwitchDebug.Log("encoderVersion: [ " + encoderVersion + " ]");
            if (encoderVersion != AvatarNow.DECODER_VERSION)
            {
                var errorMessage = "Data version isn't match.\r\n" +
                    "Encoder is V" + encoderVersion + " , decoder is V" + AvatarNow.DECODER_VERSION + " .\r\n" +
                    "Update to a newer version.";
                DecodeErrorProcess(ThumbnailDecoderErrorKind.VersionIsNotMatch, errorMessage, _callback);
                return;
            }

            //json string
            const int JSON_LENGTH_STRING_LENGTH = 6;
            var jsonLengthString = _downloadString.Substring(stringCounter, JSON_LENGTH_STRING_LENGTH);
            stringCounter += JSON_LENGTH_STRING_LENGTH;
            var jsonLength = int.Parse(jsonLengthString);
            _jsonString = _downloadString.Substring(stringCounter, jsonLength);
            stringCounter += jsonLength;
            _base64DataOffset = stringCounter;
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
                _thumbnailInfo = ThumbnailInfo.New(dic);
                EndStopWatch("Create ThumbnailInfo");
            }
            else
            {
                var errorMessage = "Deserialize information is failed";
                DecodeErrorProcess(ThumbnailDecoderErrorKind.InvalidJson, errorMessage, _callback);
                return;
            }
            _jsonString = null; //memory clean
            BranchNextMethodByProcessTime(nameof(MakeTexturesBytesPrepare));
        }

        /*----------------------------------------CreateTextureBytes-----------------------------------------*/

        int _innerNumber;
        int _innerCounter;
        public void MakeTexturesBytesPrepare()
        {
            _innerNumber = _thumbnailInfo.ThumbnailData().Length;
            _innerCounter = 0;
            _texturesBytes = new byte[_innerNumber][];
            StartStopWatch();
            MakeOneTextureBytes();
        }

        public void MakeOneTextureBytes()
        {
            StartProcessTime();
            var data = _thumbnailInfo.ThumbnailData()[_innerCounter];
            var start = _base64DataOffset + data.StartIndex();
            var length = data.DataLength();
            var textureString = _downloadString.Substring(start, length);
            _texturesBytes[_innerCounter] = Convert.FromBase64String(textureString);
            GoNextOneTextureBytes();
        }

        private void GoNextOneTextureBytes()
        {
            if (++_innerCounter < _innerNumber)
            {
                BranchNextMethodByProcessTime(nameof(MakeOneTextureBytes));
            }
            else
            {
                //end
                EndStopWatch("MakeTexturesBytes");
                BranchNextMethodByProcessTime(nameof(MakeThumbnailAndPushBuffer));
            }
        }

        public void MakeThumbnailAndPushBuffer()
        {
            //make 
            Thumbnail = GetSpriteFromtextureBytes(_texturesBytes[_targetInnerIndex]);

            //push
            var thumbnailOneBuff = ThumbnailBuff.New(_targetUrlIndex, _texturesBytes);
            _thumbnailBuff.Add(thumbnailOneBuff);

            if (_thumbnailBuff.Count() > THUMBNAIL_BUFF_NUM)
            {
                _thumbnailBuff.RemoveAt(0); //delete first
            }
            DecodeComplete();
        }

        private void DecodeComplete()
        {
            MemoryClean();
            _isBusy = false;
            Result = true;
            _callback.SendCustomEvent(_onEndCallBackName);
        }

        private void DecoderInitialize()
        {
            //callback initialize
            Result = false;
            ErrorMessage = "";
            Thumbnail = null;
            ErrorKind = ThumbnailDecoderErrorKind.None;
        }


        private Sprite GetSpriteFromtextureBytes(byte[] textureBytes)
        {
            //make sprite
            var thumbnailTexture = new Texture2D(THUMBNAIL_WITDH, THUMBNAIL_HEIGHT, THUMBNAIL_TEXTURE_FORMAT, true, false); //format is fixed
            thumbnailTexture.LoadRawTextureData(textureBytes);
            thumbnailTexture.Apply();
            thumbnailTexture.wrapMode = TextureWrapMode.Clamp;
            var imageWindow = new Rect(0, 0, THUMBNAIL_WITDH, THUMBNAIL_HEIGHT);
            return Sprite.Create(thumbnailTexture, imageWindow, Vector2.zero, THUMBNAIL_WITDH, 0, SpriteMeshType.FullRect);
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
        private void DecodeErrorProcess(ThumbnailDecoderErrorKind errorKind, string errorMessage, UdonSharpBehaviour callback)
        {
            if (errorKind != ThumbnailDecoderErrorKind.Busy)
            {
                _isBusy = false;
            }
            MemoryClean();
            Result = false;
            ErrorKind = errorKind;
            ErrorMessage = errorMessage;
            Thumbnail = null;
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