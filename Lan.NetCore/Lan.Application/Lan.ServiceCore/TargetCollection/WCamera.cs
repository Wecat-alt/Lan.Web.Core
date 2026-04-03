using Lan.ServiceCore.Onvif;
using Model;
using System.Collections.Concurrent;

namespace Lan.ServiceCore.TargetCollection
{
    public class WCamera : IDisposable
    {
        #region 成员变量
        int _nID;               //相机ID
        int _nDefenceAreaId;    //绑定防区ID
        string _ip;             //相机IP
        int _nPort;             //相机端口
        string _username;       //用户名
        string _password;       //密码

        bool _bOnline;          //是否在线
        float _fCameraHeight;   //安装高度

        float _fX;  //相机相对防区的X偏移
        float _fY;  //相机相对防区的Y偏移

        ConcurrentDictionary<IntPtr, int> _dictBindControls;    //绑定并显示摄像机画面的控件列表
        private string _name;


        public int _trackparid;
        public int _trackMode;
        public int _Counterclockwise;
        public float _minViewAngle;
        public float _viewAngle2X;
        public float _maxViewAngle;
        public int _maxZoom;
        public float _maxZoomRatio;

        public float _panLeft;
        public float _panLeftAngle;
        public float _panMiddle;
        public float _panMiddleAngle;

        public float _panMiddle2;
        public float _panMiddle2Angle;
        public float _panRight;
        public float _panRightAngle;

        public float _tiltTop;
        public float _tiltTopAngle;
        public float _tiltBottom;
        public float _tiltBottomAngle;

        public float _panOffsetAngle;
        public float _panKp;
        public float _panKi;
        public float _tiltKp;
        public float _tiltKi;
        public string _InspectionID;
        public string _InspectionName;

        public float _minZoomPan;
        public float _minZoomTilt;
        public float _maxZoomPan;
        public float _maxZoomTilt;
        public string _cameraURL;

        private int _playId;

        public bool isplay_Record = false;

        public int _IsTrack;
        #endregion

        #region 属性
        public string InspectionID
        {
            get { return _InspectionID; }
            set { _InspectionID = value; }
        }
        public string InspectionName
        {
            get { return _InspectionName; }
            set { _InspectionName = value; }
        }


        public int ID
        {
            get { return _nID; }
            internal set { _nID = value; }
        }

        public int DefenceAreaId
        {
            get { return _nDefenceAreaId; }
        }

        public string DefenceAreaName { get; set; }     //只在监控视频进程使用

        public string Ip
        {
            get { return _ip; }
            set { _ip = value; }
        }

        public int Port
        {
            get { return _nPort; }
            set { _nPort = value; }
        }

        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }


        public bool Online
        {
            get { return _bOnline; }
            set
            {
                _bOnline = value;
            }
        }

        public float CameraHeight
        {
            get { return _fCameraHeight; }
            set { _fCameraHeight = value; }
        }

        /// <summary>
        /// 相机名称
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }




        public int trackparid
        {
            get { return _trackparid; }
            set { _trackparid = value; }
        }

        public int trackMode
        {
            get { return _trackMode; }
            set { _trackMode = value; }
        }
        public int Counterclockwise
        {
            get { return _Counterclockwise; }
            set { _Counterclockwise = value; }
        }

        public float minViewAngle
        {
            get { return _minViewAngle; }
            set { _minViewAngle = value; }
        }

        public float viewAngle2X
        {
            get { return _viewAngle2X; }
            set { _viewAngle2X = value; }
        }
        public float maxViewAngle
        {
            get { return _maxViewAngle; }
            set { _maxViewAngle = value; }
        }
        public int maxZoom
        {
            get { return _maxZoom; }
            set { _maxZoom = value; }
        }

        public float maxZoomRatio
        {
            get { return _maxZoomRatio; }
            set { _maxZoomRatio = value; }
        }

        public float panLeft
        {
            get { return _panLeft; }
            set { _panLeft = value; }
        }
        public float panLeftAngle
        {
            get { return _panLeftAngle; }
            set { _panLeftAngle = value; }
        }
        public float panMiddle
        {
            get { return _panMiddle; }
            set { _panMiddle = value; }
        }
        public float panMiddleAngle
        {
            get { return _panMiddleAngle; }
            set { _panMiddleAngle = value; }
        }




        public float panMiddle2
        {
            get { return _panMiddle2; }
            set { _panMiddle2 = value; }
        }
        public float panMiddle2Angle
        {
            get { return _panMiddle2Angle; }
            set { _panMiddle2Angle = value; }
        }
        public float panRight
        {
            get { return _panRight; }
            set { _panRight = value; }
        }
        public float panRightAngle
        {
            get { return _panRightAngle; }
            set { _panRightAngle = value; }
        }



        public float tiltTop
        {
            get { return _tiltTop; }
            set { _tiltTop = value; }
        }
        public float tiltTopAngle
        {
            get { return _tiltTopAngle; }
            set { _tiltTopAngle = value; }
        }
        public float tiltBottom
        {
            get { return _tiltBottom; }
            set { _tiltBottom = value; }
        }
        public float tiltBottomAngle
        {
            get { return _tiltBottomAngle; }
            set { _tiltBottomAngle = value; }
        }









        public float minZoomPan
        {
            get { return _minZoomPan; }
            set { _minZoomPan = value; }
        }
        public float minZoomTilt
        {
            get { return _minZoomTilt; }
            set { _minZoomTilt = value; }
        }
        public float maxZoomPan
        {
            get { return _maxZoomPan; }
            set { _maxZoomPan = value; }
        }
        public float maxZoomTilt
        {
            get { return _maxZoomTilt; }
            set { _maxZoomTilt = value; }
        }
        public string cameraURL
        {
            get { return _cameraURL; }
            set { _cameraURL = value; }
        }

        public int IsTrack
        {
            get { return _IsTrack; }
            set { _IsTrack = value; }
        }


        #endregion

        protected WCamera()
        {

        }

        public string c_filename { get; set; }

        internal WCamera(CameraModel camera)
            : this()
        {
            _nID = camera.Id;
            _name = camera.Name;
            _nDefenceAreaId = camera.BindingAreaId;
            _ip = camera.Ip;
            _nPort = camera.Port;
            _username = camera.Username;
            _password = camera.Password;

            _fCameraHeight = camera.CameraHeight;

            _trackparid = camera.TrackParid;
            _trackMode = camera.TrackMode;

            _minViewAngle = camera.MinViewAngle;
            _maxViewAngle = camera.MaxViewAngle;
            _viewAngle2X = camera.ViewAngle2X;

            _maxZoom = camera.MaxZoom;
            _maxZoomRatio = camera.MaxZoomRatio;

            _panLeft = camera.PanLeft;
            _panLeftAngle = camera.PanLeftAngle;
            _panMiddle = camera.PanMiddle;
            _panMiddleAngle = camera.PanMiddleAngle;

            _panMiddle2 = camera.PanMiddle2;
            _panMiddle2Angle = camera.PanMiddle2Angle;
            _panRight = camera.PanRight;
            _panRightAngle = camera.PanRightAngle;

            _tiltTop = camera.TiltTop;
            _tiltTopAngle = camera.TiltTopAngle;
            _tiltBottom = camera.TiltBottom;
            _tiltBottomAngle = camera.TiltBottomAngle;

            _minZoomPan = camera.MinZoomPan;
            _minZoomTilt = camera.MinZoomTilt;
            _maxZoomPan = camera.MaxZoomPan;
            _maxZoomTilt = camera.MaxZoomTilt;
            _cameraURL = camera.CameraURL;
            _Counterclockwise = camera.Counterclockwise;
            _IsTrack = camera.IsTrack;
        }


        public WCamera(WCamera camera)
            : this()
        {
            _nID = -1;
            _nDefenceAreaId = -1;
            _name = camera._name;
            Ip = camera.Ip;
            Port = camera.Port;
            Username = camera.Username;
            Password = camera.Password;
        }


        ConcurrentDictionary<string, _PreviewReconrd> listVideoPreviewControl = new ConcurrentDictionary<string, _PreviewReconrd>();
        public bool StartSaveFile(string videoName, WCamera camera)
        {
            try
            {
                _PreviewReconrd _prReconrd;
                if (listVideoPreviewControl.TryGetValue(camera.Ip, out _prReconrd))
                {
                    //如果IP已经存在
                    _prReconrd.VideoPreview.BeginLocalRecord(videoName, camera, _prReconrd._playid);
                }
                else
                {
                    VideoPreview _VideoPreviewControl = new VideoPreview();
                    int playid = _VideoPreviewControl.PreviewReconrd(camera);

                    _PreviewReconrd _pr = new _PreviewReconrd();
                    _pr._playid = playid;
                    _pr.VideoPreview = _VideoPreviewControl;
                    listVideoPreviewControl.TryAdd(camera.Ip, _pr);

                    listVideoPreviewControl.TryGetValue(camera.Ip, out _prReconrd);
                    _prReconrd.VideoPreview.BeginLocalRecord(videoName, camera, _prReconrd._playid);
                }

                return true;
            }
            catch (Exception ex)
            {
                //Log.Error("StartSaveFile:" + ex.ToString());
                return false;
            }
        }
        public class _PreviewReconrd
        {
            public int _playid;
            public VideoPreview VideoPreview;
        }

        public void Dispose()
        {

        }
        public void StopSaveFile(WCamera camera)
        {

            _PreviewReconrd _prReconrd;
            if (listVideoPreviewControl.TryGetValue(camera.Ip, out _prReconrd))
            {
                _prReconrd.VideoPreview.EndLocalRecord(_prReconrd._playid);
                // Log.Debug("已停止录像" + _prReconrd._playid.ToString());
            }
            isplay_Record = false;
        }

        internal bool BindToDefenceArea(int defenceAreaId)
        {
            _nDefenceAreaId = defenceAreaId;
            return true; //SaveToDatabase();
        }
        internal bool UnBindToDefenceArea()
        {
            return BindToDefenceArea(-1);
        }

    }
}
