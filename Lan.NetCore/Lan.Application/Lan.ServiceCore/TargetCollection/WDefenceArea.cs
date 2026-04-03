using Dm.util;
using Lan.Infrastructure.Cache;
using Lan.Infrastructure.CameraOnvif;
using Lan.ServiceCore.Onvif;
using Lan.ServiceCore.Public;
using Lan.ServiceCore.Services;
using Lan.ServiceCore.WebScoket;
using Model;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using SharpJaad.AAC.Tools;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lan.ServiceCore.TargetCollection
{
    public class latlng
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    /// <summary>
    /// 防区
    /// </summary>
    public class WDefenceArea : IDisposable
    {
        #region 成员变量
        AlarmService alarmService = new AlarmService();

        /// <summary>
        /// 绘制区域相对防区的坐标
        /// </summary>
        public List<Coordinate[]> ListRadarPolygon;

        const int TrackInterval = 200;

        int _nID;
        string _name;
        int _nGroupId;
        bool _bAlgorithmEnable;
        bool _bDefenceEnable;
        bool _IsRunFormWvfMain = true;
        PointF _ptPosition;
        int _nDefenceRadius;
        bool _bOnline;
        bool _bInitialized;
        bool _bIsAlarmInHand;
        //object _lockTargets;
        WCamera _camera;
        List<WRadar> _radars;
        List<WCamera> _cameras;

        TargetCollection _targetList;           //防区轨迹列表
        //int _targetId = 0;                      //在一个防区内，报警目标的唯一标志，非负整数
        //RadarDataFilterLib.RadarDataFilter _radarTargetFilter = null;
        Stopwatch _watchAlarmHandle;

        bool _bTaskContinue;                    //目标跟踪线程允许允许标志

        int _noControlTimesForStopCamera;       //连续多少次循环中没有控制球机后，需要定位球机到最后一次控制位置
        int _cameraSwitchTimespan;				//目标切换等待的时间，在此时段内使用广角显示


        TargetPath _currentTarget;              //当前跟踪目标
        TargetPath _lastTarget;                 //上次跟踪的目标
        DateTime _trackTime;                    //目标跟踪起始时间
        DateTime _cameraSwitchTargetEndTime;    //目标切换等待的结束时间，在此时间内不执行球机控制
        DateTime _lastManualControlTime;        //用户最后一次手动控制球机的时间
        int _stateManualControl = 0;

        /// <summary>
        /// 定时器方案id
        /// </summary>
        private int _timerSchemeId;


        private RBTrackSdk.TrackCallBack _TrackCallBack;


        #endregion

        #region 属性

        #region 报警


        #endregion
        public int ScenarioID;


        public int ID
        {
            get { return _nID; }
            internal set { _nID = value; }
        }

        /// <summary>
        /// 预览窗口是否关闭
        /// </summary>
        public bool CloseFormHoverVideo
        {
            get;
            set;
        }
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public bool DefenceEnable
        {
            get
            {
                return _bDefenceEnable;
            }
            set
            {
                _bDefenceEnable = value;
            }
        }



        public int DefenceRadius
        {
            get { return _nDefenceRadius; }
            internal set { _nDefenceRadius = value; }
        }

        public bool Online
        {
            get { return _bOnline; }
            internal set { _bOnline = value; }
        }


        public WCamera Camera
        {
            get { return _camera; }
            internal set { _camera = value; }
        }
        public List<WCamera> Cameras
        {
            get { return _cameras; }
            internal set { _cameras = value; }
        }

        public List<WRadar> Radars
        {
            get { return _radars; }
            internal set { _radars = value; }
        }



        internal object TargetSyncLock { get; set; }


        public bool Alarming { get; set; }

        #endregion

        #region 构造函数
        public void RadarDefenceEnable()
        {
            foreach (WRadar _r in _radars)
            {
                _r.DefenceEnable = _bDefenceEnable;
            }
        }

        public WDefenceArea()
        {
            _lastTarget = null;
            //_maxFilter = new MaxFilter();
            _lastManualControlTime = _cameraSwitchTargetEndTime = _trackTime = DateTime.Now;

            Alarming = false;
            TargetSyncLock = new object();

            _targetList = new TargetCollection(this);

            _watchAlarmHandle = new Stopwatch();
            CloseFormHoverVideo = true;
            _bTaskContinue = true;
            Task.Factory.StartNew(trackThread).ContinueWith(t => Console.WriteLine("防区球机控制线程发生错误\r\n"
                            + t.Exception.InnerException.ToString()),
                        TaskContinuationOptions.OnlyOnFaulted);
            ListRadarPolygon = new List<Coordinate[]>();

        }

        internal WDefenceArea(DefenceareaModel defencearea)
            : this()
        {
            _nID = defencearea.Id;
            _name = defencearea.Name;

            _bDefenceEnable = defencearea.DefenceEnable == 1 ? true : false;
            float x = 0, y = 0;

            _ptPosition = new PointF(x, y);
            _nDefenceRadius = defencearea.DefenceRadius;
            _latitude = defencearea.Latitude;
            _longitude = defencearea.Longitude;

            WCamera[] cameras = CameraManager.GetInstance().GetBindingCameraOfDefenceArea(_nID);
            if (cameras.Length > 0)
            {
                _camera = cameras[0];

                _cameras = new List<WCamera>(cameras);
            }

            WRadar[] radars = RadarManager.GetInstance().GetBindingRadarOfDefenceArea(_nID);
            _radars = new List<WRadar>(radars);

            _bOnline = false;
            _bInitialized = true;
            //_MainRadar = defencearea.MainRadar;

            //RadarDefenceEnable();
            DrawPolygonService drawPolygonService = new DrawPolygonService();

            var drawPolygon = drawPolygonService.GetDrawPolygonByDefenceAreaId(_nID);
            ListRadarPolygon = ConvertListRadarPolygon(drawPolygon);

        }

        public void UpdateListRadarPolygon()
        {
            ListRadarPolygon = new List<Coordinate[]>();

            DrawPolygonService drawPolygonService = new DrawPolygonService();

            var drawPolygon = drawPolygonService.GetDrawPolygonByDefenceAreaId(_nID);
            ListRadarPolygon = ConvertListRadarPolygon(drawPolygon);
        }

        public void Dispose()
        {
            _bTaskContinue = false;
            if (_camera != null)
            {
                _camera.Dispose();
                _camera = null;
            }
        }

        #endregion


        public List<Coordinate[]> ConvertListRadarPolygon(List<DrawPolygon> ListDrawPolygon)
        {
            if (ListDrawPolygon == null || ListDrawPolygon.Count == 0)
            {
                return new List<Coordinate[]>();
            }

            foreach (DrawPolygon drawPolygon in ListDrawPolygon)
            {

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // 驼峰命名
                    PropertyNameCaseInsensitive = true, // 忽略大小写
                    WriteIndented = true
                };

                List<latlng> latlngs = System.Text.Json.JsonSerializer.Deserialize<List<latlng>>(drawPolygon.pointListLatLng, options);

                Coordinate[] polygonCoords1 = new Coordinate[latlngs.Count + 1];

                for (int i = 0; i < latlngs.Count; i++)
                {
                    Coordinate coordinate = new Coordinate();
                    coordinate.X = latlngs[i].Lat;
                    coordinate.Y = latlngs[i].Lng;
                    polygonCoords1[i] = coordinate;
                }

                Coordinate coordinateLast = new Coordinate();
                coordinateLast.X = latlngs[0].Lat;
                coordinateLast.Y = latlngs[0].Lng;
                polygonCoords1[latlngs.Count] = coordinateLast;

                ListRadarPolygon.Add(polygonCoords1);
            }
            return ListRadarPolygon;
        }



        #region 防区操作

        public bool Delete()
        {
            if (_nID > 0)
            {
                WCamera[] cameras = CameraManager.GetInstance().GetBindingCameraOfDefenceArea(_nID);
                CameraService cameraService = new CameraService();
                foreach (var cam in cameras)
                {
                    cam.BindToDefenceArea(-1);
                    cameraService.UpdateSql($"UPDATE camera SET BindingAreaId = -1 WHERE ID = '{cam.ID}'");
                }
                WRadar[] radars = RadarManager.GetInstance().GetBindingRadarOfDefenceArea(_nID);
                RadarService radarService = new RadarService();
                foreach (var radar in radars)
                {
                    radar.BindToDefenceArea(-1);
                    radarService.UpdateSql($"UPDATE radar SET BindingAreaId = -1 WHERE ID = '{radar.ID}'");
                }
                return true;
            }

            return false;
        }

        public void BindCamera(WCamera camera)
        {
            var list = CameraManager.GetInstance().GetSortedArray();
            foreach (WCamera camera1 in list)
            {
                if (_nID > 0)
                {
                    if (_camera != null && camera1.DefenceAreaId == _nID)
                    {
                        camera1.UnBindToDefenceArea();
                    }
                }
            }
            _cameras = new List<WCamera>(0);

            if (_nID > 0)
            {
                if (camera.DefenceAreaId > 0)
                {
                    try
                    {
                        WDefenceArea old = DefenceAreaManager.GetInstance()[camera.DefenceAreaId];
                        old.UnBindCamera();
                    }
                    catch { }
                }
                camera.BindToDefenceArea(_nID);
                _camera = camera;
                _cameras.Add(camera);
            }
        }

        public void UnBindCamera()
        {
            if (_nID > 0)
            {
                if (_camera != null && _camera.DefenceAreaId == _nID)
                {
                    _camera.UnBindToDefenceArea();
                }
                _camera = null;

                _cameras = new List<WCamera>(0);
            }
        }
        public void BindRadar(WRadar radar)
        {
            if (_nID > 0)
            {
                radar.Enable = true;
                radar.BindToDefenceArea(_nID);
                radar.DefenceEnable = _bDefenceEnable;

                for (int i = 0; i < _radars.Count; i++)
                    if (_radars[i].ID == radar.ID)
                    {
                        _radars[i] = radar;
                        return;
                    }
                _radars.Add(radar);
                if (!Online && radar.Online)
                    Online = true;
            }
        }

        public void UnbindRadar(WRadar radar)
        {
            if (_nID > 0 && radar.DefenceAreaId == _nID)
            {
                radar.UnBindToDefenceArea();
                for (int i = _radars.Count - 1; i >= 0; i--)
                    if (_radars[i].ID == radar.ID)
                    {
                        _radars.RemoveAt(i);
                    }
                RefreshOnlineState();
            }
        }
        public void BindRadarRange(WRadar collection)
        {
            int count = 0;

            count = _radars.Count;
            if (_nID < 0)
                return;
            for (int i = count - 1; i >= 0; i--)
            {
                _radars[i].Enable = false;
                _radars[i].UnBindToDefenceArea();
            }
            _radars.Clear();


            BindRadar(collection);

            RefreshOnlineState();
        }

        private void RefreshOnlineState()
        {
            bool online = false;
            foreach (WRadar radar in _radars)
            {
                if (radar.Online && radar.Enable)
                {
                    online = true;
                    break;
                }
            }
            Online = online;
        }

        public void UpdateAlarmTime()
        {
            _watchAlarmHandle.Restart();
        }



        #endregion

        #region 目标列表和目标跟踪



        /// <summary>
        /// 添加雷达的报警信息到队列
        /// </summary>
        /// <param name="radar"></param>
        /// <returns>是否成功添加了目标</returns>
        internal bool AddAlarmTarget(WRadar radar)
        {
            return _targetList.AddTarget(radar, ListRadarPolygon);
        }
        int newAvailableCount = 1;
        int cameraNoControlCount = 0;
        bool _bPtrsControlTour = true;//没有回到巡视点

        List<RBTRACK_Info> _list_RBTRACK = new List<RBTRACK_Info>();
        List<TargetPath> _targetPathListNew = new List<TargetPath>();       //防区当前报警目标列表

        static ConcurrentQueue<List<SendMS>> _radarTargetQueue = new ConcurrentQueue<List<SendMS>>();
        public static bool AddTarget(List<SendMS> sendMS)
        {
            _radarTargetQueue.Enqueue(sendMS);
            return true;
        }
        public void trackTargetPath()
        {
            List<SendMS> tarItem;
            while (_radarTargetQueue.TryDequeue(out tarItem))
            {
                if (!GlobalVariable.TrackStatus)
                {
                    continue;
                }

                _bPtrsControlTour = true;

                //撤防状态不执行
                if (!_bDefenceEnable)
                {
                    continue;
                }

                if (MemoryCacheHelper.Exists("RBTrack"))
                {
                    _list_RBTRACK = MemoryCacheHelper.Get<List<RBTRACK_Info>>("RBTrack");
                }

                int channelId = -1;

                WCamera[] cameras = CameraManager.GetInstance().GetBindingCameraOfDefenceArea(tarItem[0].AreaId);

                RADAR_TARGETS_T[] list_RADAR_TARGETS_T = new RADAR_TARGETS_T[tarItem.Count];
                for (int i = 0; i < tarItem.Count; i++)
                {
                    RADAR_TARGETS_T _RADAR_TARGETS_T = new RADAR_TARGETS_T();

                    _RADAR_TARGETS_T.targetId = tarItem[i].TargetId;                     //目标ID范围0x01~0x40
                    _RADAR_TARGETS_T.type = (uint)tarItem[i].TargetType;                //目标类型
                    _RADAR_TARGETS_T.speed_X = float.Parse(tarItem[i].SpeedX, CultureInfo.InvariantCulture);                      //X方向速度
                    _RADAR_TARGETS_T.speed_Y = float.Parse(tarItem[i].SpeedY, CultureInfo.InvariantCulture);                      //Y方向速度
                    _RADAR_TARGETS_T.cod_X = tarItem[i].AxesX;
                    _RADAR_TARGETS_T.cod_Y = tarItem[i].AxesY;
                    _RADAR_TARGETS_T.distance = float.Parse(tarItem[i].Distance, CultureInfo.InvariantCulture);                     //目标距离
                    _RADAR_TARGETS_T.azimuth = float.Parse(tarItem[i].AzimuthAngle, CultureInfo.InvariantCulture);                      //目标方位角

                    list_RADAR_TARGETS_T[i] = _RADAR_TARGETS_T;

                    cameraNoControlCount = 0;
                }

                if (cameras.Length > 0)
                {
                    if (_list_RBTRACK?.Count > 0)
                    {
                        for (int j = 0; j < _list_RBTRACK.Count; j++)
                        {
                            if (_list_RBTRACK[j].CameraIp == cameras[0].Ip && _list_RBTRACK[j].DefenceareaId == tarItem[0].AreaId.ToString() && _list_RBTRACK[j].channelId != -1)
                            {
                                if (cameras[0].IsTrack == 1)
                                {
                                    channelId = _list_RBTRACK[j].channelId;
                                    int s = RBTrackSdk.RBTRACK_UpdateTargets(channelId, list_RADAR_TARGETS_T, list_RADAR_TARGETS_T.Length);
                                    //Console.WriteLine($"RBTRACK_UpdateTargets channelId={channelId}, targetNum={list_RADAR_TARGETS_T.Length}, result={s}");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 防区目标跟踪线程-------------内存增加速度60KB/S
        /// </summary>
        internal void trackThread()
        {
            while (_bTaskContinue)
            {
                try
                {
                    trackTargetPath();

                }
                catch (System.Exception ex)
                {
                    //Log.Error(ex.ToString());
                }

                Thread.Sleep(TrackInterval);  //控制球机的时间间隔为200毫秒
            }
        }

        /// <summary>
        /// 球机的方位角
        /// </summary>
        public float cameraAngle;

        /// <summary>
        /// 球机当前监控距离
        /// </summary>
        public float cameraDistance = 1f;

        /// <summary>
        /// 球机视场大小
        /// </summary>
        public float cameraSight;

        /// <summary>
        /// 跟踪目标运动速度，小于0表示没有目标
        /// </summary>
        public float cameraTargetSpeed = -1;





        #endregion



        public string SearchLightIP
        {
            get { return _searchLightIp; }
            set { _searchLightIp = value; }
        }
        string _searchLightIp;

        public string angle
        {
            get { return _angle; }
            set { _angle = value; }
        }
        string _angle;

        public string Longitude
        {
            get { return _longitude; }
            set { _longitude = value; }
        }
        string _longitude;

        public string Latitude
        {
            get { return _latitude; }
            set { _latitude = value; }
        }
        string _latitude;
    }
}
