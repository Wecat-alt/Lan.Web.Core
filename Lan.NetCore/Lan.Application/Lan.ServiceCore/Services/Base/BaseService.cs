using Infrastructure;
using Lan.Model;
using Lan.Repository;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.IService.Base;
using Lan.ServiceCore.Onvif;
using Lan.ServiceCore.Public;
using Lan.ServiceCore.TargetCollection;
using Lan.ServiceCore.WebScoket;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Model;
using NetTopologySuite.Index.HPRtree;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Lan.ServiceCore.Services.Base
{
    [AppService(ServiceType = typeof(IBaseService), ServiceLifetime = LifeTime.Transient)]
    public class BaseService : IBaseService
    {
        public static void LoadCalibration()
        {
            RBTRACKManage.Init();
        }

        public static void LoadDefenceAreaAdd(int id)
        {
            CameraService cameraService = new CameraService();
            RadarService radarService = new RadarService();
            DefenceareaService defenceareaService = new DefenceareaService(radarService, cameraService);
            DefenceareaModel defenceareaModel = defenceareaService.GetInfo(id);

            //添加防区

            DefenceAreaManager.GetInstance().Add(defenceareaModel);

            WDefenceArea _defenceArea = DefenceAreaManager.GetInstance()[id];


            List<CameraModel> Cameras = cameraService.SelectCameraIds(id);

            if (Cameras.Count > 0)
            {
                WCamera cb = CameraManager.GetInstance()[Cameras[0].Ip];
                _defenceArea.BindCamera(cb);
            }


            List<RadarModel> Radars = radarService.SelectRadarIds(id);
            if (Radars.Count > 0)
            {
                WRadar _WRadar = RadarManager.GetInstance()[Radars[0].Ip];
                _defenceArea.BindRadarRange(_WRadar);
            }

        }

        public static void LoadDefenceAreaUpdate(DefenceareaModel model)
        {
            CameraService cameraService = new CameraService();
            RadarService radarService = new RadarService();

            //修改防区
            int _id = model.Id;
            string Name = model.Name;
            string DefenceEnable = model.DefenceEnable.ToString();
            string DefenceEnableName = model.DefenceEnableName;

            bool _den;
            if (DefenceEnable == "1")
                _den = true;
            else
                _den = false;

            WDefenceArea defenceArea = DefenceAreaManager.GetInstance()[_id];
            defenceArea.Name = Name;
            defenceArea.DefenceEnable = _den;
            defenceArea.DefenceRadius = model.DefenceRadius;
            defenceArea.Latitude = model.Latitude; ;
            defenceArea.Longitude = model.Longitude; ;

            //取消雷达绑定
            if (model.RadarIds?.Length > 0)
            {
                RadarModel radarModel = radarService.GetInfo(model.RadarIds[0]);
                WRadar _WRadar = RadarManager.GetInstance()[radarModel.Ip];
                defenceArea.BindRadarRange(_WRadar);
            }
            else
            {
                //如果全部清空，就要清除绑定雷达
                foreach (var item in defenceArea.Radars)
                {
                    RadarModel radarModel = radarService.GetInfo(item.ID);
                    WRadar _WRadar = RadarManager.GetInstance()[radarModel.Ip];
                    _WRadar.BindToDefenceArea(-1);
                }
                defenceArea.Radars.Clear();
                DefenceAreaManager.GetInstance().OnDefenceAreaChange(defenceArea);
            }

            if (model.CameraIds?.Length > 0)
            {
                CameraModel cameraModel = cameraService.GetInfo(model.CameraIds[0]);
                WCamera cb = CameraManager.GetInstance()[cameraModel.Ip];
                defenceArea.BindCamera(cb);
            }
            else
            {
                if (defenceArea.Cameras?.Count > 0)
                {
                    foreach (var item in defenceArea.Cameras)
                    {
                        CameraModel cameraModel = cameraService.GetInfo(item.ID);
                        WCamera cb = CameraManager.GetInstance()[cameraModel.Ip];
                        cb.UnBindToDefenceArea();
                    }
                    defenceArea.Cameras.Clear();
                }
                
            }
            RBTRACKManage.Init();

        }
        public static void LoadDefenceAreaUpdate(int status)
        {
            bool _den;
            if (status == 1)
                _den = true;
            else
                _den = false;

            foreach (WDefenceArea defence in DefenceAreaManager.GetInstance())
            {
                defence.DefenceEnable = _den;
            }
        }
        public static void LoadDefenceAreaDelete(int _id)
        {
            DefenceAreaManager.GetInstance().Delete(_id);
        }

        public static void LoadRadarAdd(int id)
        {
            RadarService radarService = new RadarService();
            var radarModel = radarService.GetInfo(id);
            //雷达添加
            RadarManager.GetInstance().Add(radarModel);
        }

        public static void LoadDeleteRadar(string ip)
        {
            //删除雷达
            string _ip = ip;

            RadarManager _radars = RadarManager.GetInstance();
            _radars.DeleteRadar(_ip);

        }

        public static void LoadCameraAdd(int _id)
        {
            CameraService cameraService = new CameraService();
            var CameraList = cameraService.GetInfo(_id);

            //相机添加
            WCamera camera = new WCamera(CameraList);
            CameraManager.GetInstance().Add(camera);
        }

        public static void LoadCameraUpdate(int _id)
        {
            GlobalVariable.TrackStatus = false;
            //修改相机
            CameraService cameraService = new CameraService();
            var CameraList = cameraService.GetInfo(_id);

            WCamera _camera = CameraManager.GetInstance()[_id];
            _camera.Name = CameraList.Name;
            _camera.Port = CameraList.Port;
            _camera.Username = CameraList.Username;
            _camera.Password = CameraList.Password;

            _camera.CameraHeight = CameraList.CameraHeight;


            _camera.trackMode = CameraList.TrackMode;
            _camera.Counterclockwise = CameraList.Counterclockwise;

            _camera.trackparid = 1;
            _camera.minViewAngle = CameraList.MinViewAngle;
            _camera.viewAngle2X = CameraList.ViewAngle2X;
            _camera.maxViewAngle = CameraList.MaxViewAngle;
            _camera.maxZoom = CameraList.MaxZoom;
            _camera.maxZoomRatio = CameraList.MaxZoomRatio;

            _camera._panLeft = CameraList.PanLeft;
            _camera._panLeftAngle = CameraList.PanLeftAngle;
            _camera._panMiddle = CameraList.PanMiddle;
            _camera._panMiddleAngle = CameraList.PanMiddleAngle;

            _camera._panMiddle2 = CameraList.PanMiddle2;
            _camera._panMiddle2Angle = CameraList.PanMiddle2Angle;
            _camera._panRight = CameraList.PanRight;
            _camera._panRightAngle = CameraList.PanRightAngle;

            _camera._tiltTop = CameraList.TiltTop;
            _camera._tiltTopAngle = CameraList.TiltTopAngle;
            _camera._tiltBottom = CameraList.TiltBottom;
            _camera._tiltBottomAngle = CameraList.TiltBottomAngle;

            _camera._panOffsetAngle = 0f;
            _camera._panKp = 0;
            _camera._panKi = 0;
            _camera._tiltKp = 0;
            _camera._tiltKi = 0;

            _camera.minZoomPan = 0;
            _camera.minZoomTilt = 0;
            _camera.maxZoomPan = 0;
            _camera.maxZoomTilt = 0;
            _camera.cameraURL = CameraList.CameraURL;
            _camera.IsTrack = CameraList.IsTrack;
            GlobalVariable.TrackStatus = true;

        }

        public static void LoadUnBindCamera(string ip)
        {

            string _ip = ip;

            foreach (WDefenceArea defence in DefenceAreaManager.GetInstance())
            {
                if (defence.Camera != null)
                {
                    if (defence.Camera.Ip == _ip)
                    {
                        defence.UnBindCamera();
                        DefenceAreaManager.GetInstance().OnDefenceAreaChange(defence);
                    }
                }
            }

            WCamera cb = CameraManager.GetInstance()[_ip];

            CameraManager.GetInstance().Remove(cb);

        }
        public static void LoadUnBindCamera(string status, string ip)
        {
            if (status == "15")
            {
                string _ip = ip;

                foreach (WDefenceArea defence in DefenceAreaManager.GetInstance())
                {
                    if (defence.Camera != null)
                    {
                        if (defence.Camera.Ip == _ip)
                        {
                            defence.UnBindCamera();
                            DefenceAreaManager.GetInstance().OnDefenceAreaChange(defence);
                        }
                    }
                }
            }
        }
    }
}
