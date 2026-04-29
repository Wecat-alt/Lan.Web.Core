
using Infrastructure;
using Lan.Infrastructure.CameraOnvif;
using Lan.ServiceCore.Public;
using Lan.ServiceCore.Services;
using Lan.ServiceCore.Signalr;
using MemoryCache.Core;
using Model;
using NetTopologySuite.Index.HPRtree;
using SqlSugar.IOC;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Lan.ServiceCore.Onvif
{
    public class RBTRACK_Info
    {
        public int channelId { get; set; }
        public string DefenceareaId { get; set; }
        public string CameraIp { get; set; }
    }
    public class RBTRACKManage
    {
        public static List<RBTRACK_Info> list_RBTRACK = new List<RBTRACK_Info>();

        static CameraService cameraService = new CameraService();
        CalibrationService calibrationService = new CalibrationService();
        private static RBTrackSdk.TrackCallBack _TrackCallBack;

        private static readonly IMemoryCacheService? _cache = App.GetService<IMemoryCacheService>();


        public static void Init()
        {
            GlobalVariable.TrackStatus = false;
            if (_TrackCallBack is null)
            {
                _TrackCallBack = TrackCallBack;
            }

            LoadONVIF_RBTRACK();

            GlobalVariable.TrackStatus = true;
        }
        private static void LoadONVIF_RBTRACK()
        {
            if (_cache.Exists("RBTrack"))
            {
                if (list_RBTRACK.Count > 0)
                {
                    for (int i = 0; i < list_RBTRACK.Count; i++)
                    {
                        int r = RBTrackSdk.RBTRACK_DeleteTrack(list_RBTRACK[i].channelId);
                    }
                    list_RBTRACK = new List<RBTRACK_Info>();
                }
                _cache.Remove("RBTrack");
            }
            List<CameraModel> cameraBuffers = cameraService.GetAllList();


            if (cameraBuffers != null)
            {
                foreach (CameraModel item in cameraBuffers)
                {
                    if (!string.IsNullOrEmpty(item.CameraURL))
                    {
                        ONVIF_ANGLE_MAP_T panLeftToMiddle = new ONVIF_ANGLE_MAP_T();
                        panLeftToMiddle.fromOnvifVal = item.PanLeft; //从ONVIF值
                        panLeftToMiddle.toOnvifVal = item.PanMiddle;  //到ONVIF值
                        panLeftToMiddle.fromAngle = item.PanLeftAngle;	//从球机角度
                        panLeftToMiddle.toAngle = item.PanMiddleAngle;   //到球机角度

                        ONVIF_ANGLE_MAP_T panMiddleToRight = new ONVIF_ANGLE_MAP_T();
                        panMiddleToRight.fromOnvifVal = item.PanMiddle2; //从ONVIF值
                        panMiddleToRight.toOnvifVal = item.PanRight;  //到ONVIF值
                        panMiddleToRight.fromAngle = item.PanMiddle2Angle;	//从球机角度
                        panMiddleToRight.toAngle = item.PanRightAngle;   //到球机角度  float.Parse(Camera.tiltTopToBottom.Split(',')[1]);   //到球机角度

                        ONVIF_ANGLE_MAP_T tiltTopToBottom = new ONVIF_ANGLE_MAP_T();
                        tiltTopToBottom.fromOnvifVal = item.TiltTop; //从ONVIF值
                        tiltTopToBottom.toOnvifVal = item.TiltBottom;  //到ONVIF值
                        tiltTopToBottom.fromAngle = item.TiltTopAngle;	//从球机角度
                        tiltTopToBottom.toAngle = item.TiltBottomAngle;

                        BALL_PARA_T _BALL_PARA_T = new BALL_PARA_T();
                        _BALL_PARA_T.trackMode = item.TrackMode;
                        _BALL_PARA_T.viewAngle1X = item.MinViewAngle;
                        _BALL_PARA_T.viewAngle2X = item.ViewAngle2X;
                        _BALL_PARA_T.viewAngleMaxX = item.MaxViewAngle;
                        _BALL_PARA_T.maxZoom = item.MaxZoom;
                        _BALL_PARA_T.maxZoomRatio = 1920.0f * 0.5f / item.MaxZoomRatio;// ;
                        _BALL_PARA_T.panCounterclockwise = item.Counterclockwise;
                        _BALL_PARA_T.panLeftToMiddle = panLeftToMiddle;
                        _BALL_PARA_T.panMiddleToRight = panMiddleToRight;
                        _BALL_PARA_T.tiltTopToBottom = tiltTopToBottom;

                        _BALL_PARA_T.panKp = 0.0f;
                        _BALL_PARA_T.panKi = 0.0f;
                        _BALL_PARA_T.tiltKp = 0.0f;
                        _BALL_PARA_T.tiltKi = 0.0f;
                        ////防区雷达=相机绑定雷达
                        int cameraid = item.CameraId;

                        CalibrationService calibrationService = new CalibrationService();

                        Calibration calibration = calibrationService.GetInfo(item.Ip, item.BindingAreaId);

                        if (calibration != null)
                        {
                            //如果是主雷达
                            _BALL_PARA_T.ballPointX = calibration.CameraPointX;
                            _BALL_PARA_T.ballPointY = calibration.CameraPointY;

                            _BALL_PARA_T.referPointX = 0f;
                            _BALL_PARA_T.referPointY = calibration.CalibrationDistance; //30f;
                            _BALL_PARA_T.referPointPan = calibration.CamerarPointAngle;

                            _BALL_PARA_T.minZoomPan = item.MinZoomPan;
                            _BALL_PARA_T.minZoomTilt = item.MinZoomTilt;
                            _BALL_PARA_T.maxZoomPan = item.MaxZoomPan;
                            _BALL_PARA_T.maxZoomTilt = item.MaxZoomTilt;

                        }
                        else
                        {
                            _BALL_PARA_T.ballPointX = 0f;
                            _BALL_PARA_T.ballPointY = 0f;
                            _BALL_PARA_T.referPointX = 0f;
                            _BALL_PARA_T.referPointY = 30f;
                            _BALL_PARA_T.referPointPan = 0f;
                            _BALL_PARA_T.minZoomPan = 0;
                            _BALL_PARA_T.minZoomTilt = 0;
                            _BALL_PARA_T.maxZoomPan = 0;
                            _BALL_PARA_T.maxZoomTilt = 0;
                            continue;
                        }

                        _BALL_PARA_T.installHeight = item.CameraHeight - 0.5f;

                        int channelId = RBTrackSdk.RBTRACK_CreateTrack(item.Ip, item.Username, item.Password, cameraid, ref _BALL_PARA_T, _TrackCallBack, IntPtr.Zero);

                        if (channelId != -1)
                        {
                            //设置跟踪模式
                            int s = RBTrackSdk.RBTRACK_SetTrackMode(channelId, eTrackMode.eTrack_Polling);

                            RBTRACK_Info ri = new RBTRACK_Info();
                            ri.channelId = channelId;
                            ri.CameraIp = item.Ip;
                            ri.DefenceareaId = item.BindingAreaId.ToString();

                            list_RBTRACK.Add(ri);
                        }
                    }
                }
                if (list_RBTRACK.Count > 0)
                {
                    _cache.Set("RBTrack", list_RBTRACK);
                }
            }
        }
        private static int TrackCallBack(int channelId, ref RADAR_TARGETS_T target, IntPtr userPtr)
        {
            try
            {
                TrackTarget.AddTrackTarget(target.targetId);
            }
            catch
            {
            }
            return 0;
        }
    }
}
