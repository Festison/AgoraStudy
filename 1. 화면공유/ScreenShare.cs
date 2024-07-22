using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Agora.Rtc;
 
using UnityEngine.Serialization;
 

namespace Agora_RTC_Plugin.API_Example.Examples.Advanced.ScreenShare
{
    public class ScreenShare : MonoBehaviour
    {
        [FormerlySerializedAs("appIdInput")]
        [SerializeField]
        private AppIdInput _appIdInput;

        [Header("_____________Basic Configuration_____________")]
        [FormerlySerializedAs("APP_ID")]
        [SerializeField]
        private string _appID = "";

        [FormerlySerializedAs("TOKEN")]
        [SerializeField]
        private string _token = "";

        [FormerlySerializedAs("CHANNEL_NAME")]
        [SerializeField]
        private string _channelName = "";

        public Text LogText;
        internal Logger Log;
        internal IRtcEngine RtcEngine = null;
        private ScreenCaptureSourceInfo[] _screenCaptureSourceInfos;

        public Dropdown WinIdSelect;
        public Button GetSourceBtn;
        public Button StartShareBtn;
        public Button StopShareBtn;
        public Button UpdateShareBtn;
        public Button PublishBtn;
        public Button UnpublishBtn;
        public Button ShowThumbBtn;
        public Button ShowIconBtn;

        private Rect _originThumRect = new Rect(0,0,500,260);
        private Rect _originIconRect = new Rect(0,0,289,280);

        public GameObject screenSharePrefab;

        // Use this for initialization
        protected void  Start()
        {
            LoadAssetData();
            if (CheckAppId())
            {
                InitEngine();
                SetBasicConfiguration();
#if UNITY_ANDROID || UNITY_IPHONE
                GetSourceBtn.gameObject.SetActive(false);
                WinIdSelect.gameObject.SetActive(false);
                UpdateShareBtn.gameObject.SetActive(true);
                IconImage.gameObject.SetActive(false);
                ThumbImage.gameObject.SetActive(false);
                ShowThumbBtn.gameObject.SetActive(false);
                ShowIconBtn.gameObject.SetActive(false);
#else
                UpdateShareBtn.gameObject.SetActive(false);
#endif
            }
        }

        #region 신경 안써도되는 초기화 부분
        private bool CheckAppId()
        {
            Log = new Logger(LogText);
            return Log.DebugAssert(_appID.Length > 10, "Please fill in your appId in API-Example/profile/appIdInput.asset");
        }

        //Show data in AgoraBasicProfile
        [ContextMenu("ShowAgoraBasicProfileData")]
        private void LoadAssetData()
        {
            if (_appIdInput == null) return;
            _appID = _appIdInput.appID;
            _token = _appIdInput.token;
            _channelName = _appIdInput.channelName;
        }

        private void InitEngine()
        {
            RtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngine();
            UserEventHandler handler = new UserEventHandler(this);
            RtcEngineContext context = new RtcEngineContext(_appID, 0,
                                        CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING,
                                        AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT);
            RtcEngine.Initialize(context);
            RtcEngine.InitEventHandler(handler);
        }

        private void SetBasicConfiguration()
        {
            RtcEngine.EnableAudio();
            RtcEngine.EnableVideo();
            RtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        }
        #endregion

        #region -- Button Events ---

        public void JoinChannel(string _channelNameSet)
        {
            RtcEngine.JoinChannel(_token, _channelNameSet);
            _channelName= _channelNameSet; 
        }

        public void LeaveChannel()
        {
            RtcEngine.LeaveChannel();
        }

        public void OnPublishButtonClick()
        {
            ChannelMediaOptions options = new ChannelMediaOptions();
            options.publishCameraTrack.SetValue(false);
            options.publishScreenTrack.SetValue(true);

#if UNITY_ANDROID || UNITY_IPHONE
            options.publishScreenCaptureAudio.SetValue(true);
            options.publishScreenCaptureVideo.SetValue(true);
#endif
            var ret = RtcEngine.UpdateChannelMediaOptions(options);

            PublishBtn.gameObject.SetActive(false);
            UnpublishBtn.gameObject.SetActive(true);
            Debug.Log("퍼블리시 시작");
        }

        public void OnUnplishButtonClick()
        {
            ChannelMediaOptions options = new ChannelMediaOptions();
            options.publishCameraTrack.SetValue(true);
            options.publishScreenTrack.SetValue(false);

#if UNITY_ANDROID || UNITY_IPHONE
            options.publishScreenCaptureAudio.SetValue(false);
            options.publishScreenCaptureVideo.SetValue(false);
#endif
            var ret = RtcEngine.UpdateChannelMediaOptions(options);
            Debug.Log("UpdateChannelMediaOptions returns: " + ret);

            PublishBtn.gameObject.SetActive(true);
            UnpublishBtn.gameObject.SetActive(false);
        }

        // 캡처 부분 신경 x
        public void PrepareScreenCapture()
        {
            if (WinIdSelect == null || RtcEngine == null) return;

            WinIdSelect.ClearOptions();

            SIZE t = new SIZE();
            t.width = 1280;
            t.height = 720;
            SIZE s = new SIZE();
            s.width = 1200;
            s.height = 1200;
            _screenCaptureSourceInfos = RtcEngine.GetScreenCaptureSources(t, s, true);

            WinIdSelect.AddOptions(_screenCaptureSourceInfos.Select(w =>
                    new Dropdown.OptionData(
                        string.Format("{0}: {1}-{2} | {3}", w.type, w.sourceName, w.sourceTitle, w.sourceId)))
                .ToList());
        }

        //
        public void OnStartShareBtnClick()
        {
            if (RtcEngine == null) return;

            if (WinIdSelect == null) return;
            var option = WinIdSelect.options[WinIdSelect.value].text;
            if (string.IsNullOrEmpty(option)) return;

            if (option.Contains("ScreenCaptureSourceType_Window"))
            {
                var windowId = option.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1];
                Log.UpdateLog(string.Format(">>>>> Start sharing {0}", windowId));
                var nRet = RtcEngine.StartScreenCaptureByWindowId(long.Parse(windowId), default(Rectangle),
                        default(ScreenCaptureParameters));
                this.Log.UpdateLog("StartScreenCaptureByWindowId:" + nRet);
            }
            else
            {
                var dispId = uint.Parse(option.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]);
                Log.UpdateLog(string.Format(">>>>> Start sharing display {0}", dispId));
                var nRet = RtcEngine.StartScreenCaptureByDisplayId(dispId, default(Rectangle),
                    new ScreenCaptureParameters { captureMouseCursor = true, frameRate = 30 });
                this.Log.UpdateLog("StartScreenCaptureByDisplayId:" + nRet);
            }

            RtcEngine.StopScreenCapture();
            OnPublishButtonClick();
            ScreenShare.MakeVideoView(0, "", VIDEO_SOURCE_TYPE.VIDEO_SOURCE_SCREEN);
            Debug.Log("공유 시작");
        }

        public void OnStartShareBtnClick(long dispId, string roomid, ScreenCaptureSourceType type)
        {
            if (RtcEngine == null) return;

            var captureParams = new ScreenCaptureParameters
            {
                frameRate = 30,
                bitrate = 800,
                captureMouseCursor = true,
            };

            RtcEngine.StopScreenCapture();

            if (type == ScreenCaptureSourceType.ScreenCaptureSourceType_Window)
            {
                var ret = RtcEngine.StartScreenCaptureByWindowId(dispId, default(Rectangle), captureParams);
                if (ret == 0) { Debug.Log("캡처 성공"); }
            }           
            else
            {
                var nRet = RtcEngine.StartScreenCaptureByDisplayId((uint)dispId, default(Rectangle),
                    new ScreenCaptureParameters { captureMouseCursor = true, frameRate = 30 });
            }

            OnPublishButtonClick();
            ScreenShare.MakeVideoViewSet(0, "", VIDEO_SOURCE_TYPE.VIDEO_SOURCE_SCREEN);
            Debug.Log("공유 시작");
        }


        public void OnUpdateShareBtnClick()
        {
            //only work in ios or android
            var config = new ScreenCaptureParameters2();
            config.captureAudio = true;
            config.captureVideo = true;
            config.videoParams.dimensions.width = 960;
            config.videoParams.dimensions.height = 640;
            var nRet = RtcEngine.UpdateScreenCapture(config);
            this.Log.UpdateLog("UpdateScreenCapture: " + nRet);
        }

        #endregion

        private void OnDestroy()
        {
            Debug.Log("OnDestroy");
            if (RtcEngine == null) return;
            RtcEngine.InitEventHandler(null);
            RtcEngine.LeaveChannel();
            RtcEngine.Dispose();
        }

        internal string GetChannelName()
        {
            return _channelName;
        }

        #region -- Video Render UI Logic ---

        internal static void MakeVideoView(uint uid, string channelId = "", VIDEO_SOURCE_TYPE videoSourceType = VIDEO_SOURCE_TYPE.VIDEO_SOURCE_CAMERA)
        {
            var go = GameObject.Find(uid.ToString());
            if (!ReferenceEquals(go, null))
            {
                return; // reuse
            }

            // create a GameObject and assign to this new user
            var videoSurface = MakeImageSurface(uid.ToString());
            if (ReferenceEquals(videoSurface, null))
            {
                return;
            }
            
            // configure videoSurface
            videoSurface.SetForUser(uid, channelId, videoSourceType);
            videoSurface.SetEnable(true);

            videoSurface.OnTextureSizeModify += (int width, int height) =>
            {
                float scale = (float)height / (float)width;
                videoSurface.transform.localScale = new Vector3(-12.6f, 15 * scale, 1);
            };
        }
        //추가
        internal static void MakeVideoViewSet(uint uid, string channelId = "", VIDEO_SOURCE_TYPE videoSourceType = VIDEO_SOURCE_TYPE.VIDEO_SOURCE_CAMERA)
        {
            // 원인 찾음 이녀석 null뜸 지금 구조는 uid 게임오브젝트가 0으로 고정되는 구조
            var go = GameObject.Find(uid.ToString());

            var videoSurface = go.GetComponent<VideoSurface>();
            if (ReferenceEquals(videoSurface, null)) return;
            // configure videoSurface
            videoSurface.SetForUser(uid, channelId, videoSourceType);
            videoSurface.SetEnable(true);
        }

        // Video TYPE 2: RawImage
        // 만드는 곳
        private static VideoSurface MakeImageSurface(string goName)
        {
            var go = new GameObject();

            if (go == null)
            {
                return null;
            }

            go.name = goName;
            // to be renderered onto
            go.AddComponent<RawImage>();
            // make the object draggable
            var canvas = GameObject.Find("VideoCanvas");
            if (canvas != null)
            {
                go.transform.parent = canvas.transform;
                go.transform.SetAsFirstSibling();
                Debug.Log("add video view");
            }
            else
            {
                Debug.Log("Canvas is null video view");
            }

            // set up transform
            go.transform.Rotate(0f, 0.0f, 180.0f);
            go.transform.localPosition = Vector3.zero;
            
            go.transform.localScale = new Vector3(5f, 5f, 1f);

            // configure videoSurface
            var videoSurface = go.AddComponent<VideoSurface>();
            return videoSurface;
        }

        internal static void DestroyVideoView(uint uid)
        {
            var go = GameObject.Find(uid.ToString());
            if (!ReferenceEquals(go, null))
            {
                Destroy(go);
            }
        }

        

        public DisplaySet[] CaptureView()
        {
            SIZE t = new SIZE();
            t.width = 1920;
            t.height = 1080;
            SIZE s = new SIZE();
            s.width = 100;
            s.height = 100;
            //RtcEngine.GetVideoDeviceManager();
            ScreenCaptureSourceInfo[] info = RtcEngine.GetScreenCaptureSources(t, s, false);
            int count = info.Length; //  GetScreenCaptureSourcesCount
            DisplaySet[] newTex = new DisplaySet[count];

            for (int i = 0; i < count; i++)
            {
                ScreenCaptureSourceInfo item = info[i];
                newTex[i].id = item.sourceId;
                newTex[i].type = item.type;
                var ptr = item.thumbImage;
                Texture2D ptrImage = new Texture2D((int)ptr.width, (int)ptr.height, TextureFormat.BGRA32, false);

                ptrImage.LoadRawTextureData(ptr.buffer);

                ptrImage.Apply();
                newTex[i].tex = ptrImage;
            }

            return newTex;
        }
        #endregion
    }

#region -- Agora Event ---

    internal class UserEventHandler : IRtcEngineEventHandler
    {
        private readonly ScreenShare _desktopScreenShare;

        internal UserEventHandler(ScreenShare desktopScreenShare)
        {
            _desktopScreenShare = desktopScreenShare;
        }

        public override void OnError(int err, string msg)
        {
            _desktopScreenShare.Log.UpdateLog(string.Format("OnError err: {0}, msg: {1}", err, msg));
        }

        public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
        {
            int build = 0;
            _desktopScreenShare.Log.UpdateLog(string.Format("sdk version: ${0}",
                _desktopScreenShare.RtcEngine.GetVersion(ref build)));
            _desktopScreenShare.Log.UpdateLog(
                string.Format("OnJoinChannelSuccess channelName: {0}, uid: {1}, elapsed: {2}",
                                connection.channelId, connection.localUid, elapsed));
            Debug.Log("OnUserJionedSuccess 호출");
        }

        public override void OnRejoinChannelSuccess(RtcConnection connection, int elapsed)
        {
            _desktopScreenShare.Log.UpdateLog("OnRejoinChannelSuccess");
        }

        public override void OnLeaveChannel(RtcConnection connection, RtcStats stats)
        {
            _desktopScreenShare.Log.UpdateLog("OnLeaveChannel");
            ScreenShare.DestroyVideoView(0);
        }

        public override void OnClientRoleChanged(RtcConnection connection, CLIENT_ROLE_TYPE oldRole, CLIENT_ROLE_TYPE newRole, ClientRoleOptions newRoleOptions)
        {
            _desktopScreenShare.Log.UpdateLog("OnClientRoleChanged");
        }

        public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
        {
            var Video = GameObject.Find("0");

            if (Video != null)
            {
                Video.name = uid.ToString();
            }

            ScreenShare.MakeVideoViewSet(uid, connection.channelId, VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE);
            Debug.Log("OnUserJioned 호출");
        }

        public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
        {
            _desktopScreenShare.Log.UpdateLog(string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid,
                (int)reason));
            ScreenShare.DestroyVideoView(uid);
        }
    }

#endregion
}