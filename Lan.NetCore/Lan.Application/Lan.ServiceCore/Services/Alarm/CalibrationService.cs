using Infrastructure;
using Lan.Infrastructure.CameraOnvif;
using Lan.Repository;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.Onvif;
using Lan.ServiceCore.Public;
using Lan.ServiceCore.Services.Base;
using Lan.ServiceCore.WebScoket;
using MemoryCache.Core;
using Model;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Lan.ServiceCore.Services
{
    [AppService(ServiceType = typeof(ICalibrationService), ServiceLifetime = LifeTime.Transient)]
    public class CalibrationService : Repository<Calibration>, ICalibrationService
    {
        ONVIF_COMMON_INFO common = new ONVIF_COMMON_INFO();
        private readonly IMemoryCacheService? _cache;
        public CalibrationService(IMemoryCacheService? cache = null)
        {
            _cache = cache;
        }



        public string AddCalibration(Calibration model)
        {
            Queryable().Where(f => f.CameraIp == model.CameraIp && f.DefenceareaId == model.DefenceareaId).ToList().ForEach(item => { Delete(item.Id); });

            //common = MemoryCacheHelper.Get<ONVIF_COMMON_INFO>(model.CameraIp);
            common = _cache.Get<ONVIF_COMMON_INFO>(model.CameraIp);

            ONVIF_PTZ_STATUS panPost = new ONVIF_PTZ_STATUS();
            int ret = onvifsdk.ONVIF_PTZ_GetStatus(2, ref common, ref panPost);

            model.CamerarPointAngle = panPost.panPosition;
            Insertable(model).ExecuteReturnEntity();

            BaseService.LoadCalibration();
            return $"Point:{model.CamerarPointAngle:F2}";
        }

        public void cameraPTZ(string _CameraPTZ, string ip, string speed)
        {
            ONVIF_PTZ_CONTINUSMOVE ONVIF_PTZ_CONTINUSMOVE = new ONVIF_PTZ_CONTINUSMOVE();

            //ONVIF_PTZ_ABSOLUTEMOVE oNVIF_PTZ_ABSOLUTEMOVE = new ONVIF_PTZ_ABSOLUTEMOVE();
            //oNVIF_PTZ_ABSOLUTEMOVE.panSpeed=1;     //水平速度 	[0, 1]
            //oNVIF_PTZ_ABSOLUTEMOVE.tiltSpeed=1;    //垂直速度 	[0, 1]
            //oNVIF_PTZ_ABSOLUTEMOVE.zoomSpeed=1;    //变倍速度  [0, 1]
            //oNVIF_PTZ_ABSOLUTEMOVE.panPosition=0;  //水平位置  [-1, 1]
            //oNVIF_PTZ_ABSOLUTEMOVE.tiltPosition=0;  //垂直位置 [-1, 1] 若只想绝对变倍，panPosition、tiltPosition可设置为-2.0;
            //oNVIF_PTZ_ABSOLUTEMOVE.zoomPosition=0;   //变倍变数 [0, 1] 若只想水平垂直运动，zoomPosition可设置为-2.0; 


            //common = MemoryCacheHelper.Get<ONVIF_COMMON_INFO>(ip);
            common = _cache.Get<ONVIF_COMMON_INFO>(ip);
            float onvif_speed = float.Parse(speed, CultureInfo.InvariantCulture);
            switch (_CameraPTZ)
            {
                case "top":
                    ONVIF_PTZ_CONTINUSMOVE.panSpeed = 0;
                    ONVIF_PTZ_CONTINUSMOVE.tiltSpeed = onvif_speed;// 0.5f;
                    int ret = onvifsdk.ONVIF_PTZ_ContinusMove(2, ref common, ref ONVIF_PTZ_CONTINUSMOVE);
                    break;
                case "topright":
                    ONVIF_PTZ_CONTINUSMOVE.panSpeed = onvif_speed;//0.5f;
                    ONVIF_PTZ_CONTINUSMOVE.tiltSpeed = onvif_speed;//0.5f;
                    ret = onvifsdk.ONVIF_PTZ_ContinusMove(2, ref common, ref ONVIF_PTZ_CONTINUSMOVE);
                    break;
                case "right":
                    ONVIF_PTZ_CONTINUSMOVE.panSpeed = onvif_speed;//0.5f;
                    ONVIF_PTZ_CONTINUSMOVE.tiltSpeed = 0;
                    ret = onvifsdk.ONVIF_PTZ_ContinusMove(2, ref common, ref ONVIF_PTZ_CONTINUSMOVE);
                    break;
                case "bottomright":
                    ONVIF_PTZ_CONTINUSMOVE.panSpeed = onvif_speed;//0.5f;
                    ONVIF_PTZ_CONTINUSMOVE.tiltSpeed = -onvif_speed;// -0.5f;
                    ret = onvifsdk.ONVIF_PTZ_ContinusMove(2, ref common, ref ONVIF_PTZ_CONTINUSMOVE);
                    break;
                case "bottom":
                    ONVIF_PTZ_CONTINUSMOVE.panSpeed = 0;
                    ONVIF_PTZ_CONTINUSMOVE.tiltSpeed = -onvif_speed;//-0.5f;
                    ret = onvifsdk.ONVIF_PTZ_ContinusMove(2, ref common, ref ONVIF_PTZ_CONTINUSMOVE);
                    break;
                case "bottomleft":
                    ONVIF_PTZ_CONTINUSMOVE.panSpeed = -onvif_speed;//-0.5f;
                    ONVIF_PTZ_CONTINUSMOVE.tiltSpeed = -onvif_speed;//-0.5f;
                    ret = onvifsdk.ONVIF_PTZ_ContinusMove(2, ref common, ref ONVIF_PTZ_CONTINUSMOVE);
                    break;
                case "back":
                    ONVIF_PTZ_CONTINUSMOVE.panSpeed = -onvif_speed;//-0.5f;
                    ONVIF_PTZ_CONTINUSMOVE.tiltSpeed = 0;
                    ret = onvifsdk.ONVIF_PTZ_ContinusMove(2, ref common, ref ONVIF_PTZ_CONTINUSMOVE);
                    break;
                case "topleft":
                    ONVIF_PTZ_CONTINUSMOVE.panSpeed = -onvif_speed;//-0.5f;
                    ONVIF_PTZ_CONTINUSMOVE.tiltSpeed = onvif_speed;//0.5f;
                    ret = onvifsdk.ONVIF_PTZ_ContinusMove(2, ref common, ref ONVIF_PTZ_CONTINUSMOVE);
                    break;
                case "outin":
                    ONVIF_PTZ_CONTINUSMOVE.panSpeed = 0;
                    ONVIF_PTZ_CONTINUSMOVE.tiltSpeed = 0;
                    ONVIF_PTZ_CONTINUSMOVE.zoomSpeed = -1;
                    ret = onvifsdk.ONVIF_PTZ_ContinusMove(2, ref common, ref ONVIF_PTZ_CONTINUSMOVE);
                    break;
                case "in":
                    ONVIF_PTZ_CONTINUSMOVE.panSpeed = 0;
                    ONVIF_PTZ_CONTINUSMOVE.tiltSpeed = 0;
                    ONVIF_PTZ_CONTINUSMOVE.zoomSpeed = 1;
                    ret = onvifsdk.ONVIF_PTZ_ContinusMove(2, ref common, ref ONVIF_PTZ_CONTINUSMOVE);
                    break;

            }
        }

        public void PtzStop(string ip)
        {
            ONVIF_PTZ_CONTINUSMOVE ONVIF_PTZ_CONTINUSMOVE = new ONVIF_PTZ_CONTINUSMOVE();
            //common = MemoryCacheHelper.Get<ONVIF_COMMON_INFO>(ip);
            common = _cache.Get<ONVIF_COMMON_INFO>(ip);
            onvifsdk.ONVIF_PTZ_ContinusStop(2, ref common);
        }

        public Calibration GetInfo(string CameraIp, int DefenceareaId) => Queryable().Where(x => x.CameraIp == CameraIp && x.DefenceareaId == DefenceareaId).First();

        public int UpdateCalibration(string CameraIp, int DefenceareaId, float CalibrationDistance, float CameraPointX, float CameraPointY, float CamerarPointAngle)
        {
            GlobalVariable.TrackStatus = false;
            int i = UpdateSql($"UPDATE calibration SET CalibrationDistance = {CalibrationDistance},CameraPointX={CameraPointX},CameraPointY={CameraPointY},CamerarPointAngle={CamerarPointAngle} WHERE CameraIp = '{CameraIp}' and DefenceareaId='{DefenceareaId}'");
            BaseService.LoadCalibration();
            GlobalVariable.TrackStatus = true;
            return i;
        }
        public int UpdateSql(string sql)
        {
            return SqlCommand(sql);
        }
    }
}
