using Lan.Infrastructure.CameraOnvif;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.Public;
using Lan.ServiceCore.Services.Base;
using Lan.ServiceCore.TargetCollection;
using MemoryCache.Core;
using System.Text;

namespace Lan.ServiceCore.Services
{
    [AppService(ServiceType = typeof(ICameraService), ServiceLifetime = LifeTime.Singleton)]
    public class CameraService : Repository<CameraModel>, ICameraService
    {
        ONVIF_COMMON_INFO common = new ONVIF_COMMON_INFO();
        private readonly IMemoryCacheService? _cache;
        public CameraService(IMemoryCacheService? cache = null)
        {
            _cache = cache;
        }

        public List<CameraModel> GetList(CameraQueryDto parm)
        {
            var predicate = QueryExp(parm);

            var exp = Expressionable.Create<CameraModel>();
            exp.AndIF(!string.IsNullOrEmpty(parm.ip), u => u.Ip.Contains(parm.ip));
            exp.AndIF(!string.IsNullOrEmpty(parm.defenceareaId.ToString()), u => u.BindingAreaId == parm.defenceareaId);

            var response = Queryable()
                .Where(exp.ToExpression()).ToList();

            foreach (var camera in response)
            {
                //common = MemoryCacheHelper.Get<ONVIF_COMMON_INFO>(camera.Ip);
                common = _cache.Get<ONVIF_COMMON_INFO>(camera.Ip);
                ONVIF_DEVICE_INFO oNVIF_DEVICE_INFO = new ONVIF_DEVICE_INFO();
                int ret = onvifsdk.ONVIF_MAGEMENT_GetDeviceInformation(10, ref common, ref oNVIF_DEVICE_INFO);
                if (ret == 0)
                {
                    camera.Online = true;
                    camera.Manufacturer = oNVIF_DEVICE_INFO.manufacturer is { Length: > 0 } ? Encoding.Default.GetString(oNVIF_DEVICE_INFO.manufacturer).TrimEnd('\0') : string.Empty;
                    camera.DeviceTypeName = oNVIF_DEVICE_INFO.model is { Length: > 0 } ? Encoding.Default.GetString(oNVIF_DEVICE_INFO.model).TrimEnd('\0') : string.Empty;
                }
                else
                {
                    camera.Online = false;
                }
            }

            return response;
        }

        private static Expressionable<CameraModel> QueryExp(CameraQueryDto parm)
        {
            var predicate = Expressionable.Create<CameraModel>();

            return predicate;
        }


        public Task<List<CameraModel>> GetListPreviewAsync()
        {
            return Queryable()
                .LeftJoin<DefenceareaModel>((u, c) => u.BindingAreaId == c.Id)  // 修正：包含所有已连接的表
                .Select((u, c) => new CameraModel
                {
                    Id = u.Id,
                    BindingAreaId = u.BindingAreaId,
                    Ip = u.Ip,
                    Port = u.Port,
                    CameraURL = u.CameraURL,
                    Username = u.Username,
                    Password = u.Password,
                    ZoneName = c.Name,
                })
                .ToListAsync();
        }

        public CameraModel GetInfo(int Id)
        {
            var response = Queryable()
                .Where(x => x.Id == Id)
                .First();
            return response;
        }
        public CameraModel GetInfo(string Ip)
        {
            var response = Queryable()
                .Where(x => x.Ip == Ip)
                .First();
            return response;
        }
        public bool GetInfoByIp(string Ip)
        {
            bool bl = Queryable().Any(x => x.Ip == Ip);
            return bl;
        }
        public int AddCamera(CameraModel model)
        {
            GlobalVariable.TrackStatus = false;
            int id = Insertable(model).ExecuteReturnIdentity();
            BaseService.LoadCameraAdd(id);
            GlobalVariable.TrackStatus = true;
            return id;
        }

        public int UpdateCamera(CameraModel model)
        {
            GlobalVariable.TrackStatus = false;
            int i = Update(model, true);
            BaseService.LoadCameraUpdate(model.Id);
            BaseService.LoadCalibration();
            GlobalVariable.TrackStatus = true;
            return i;
        }
        public int UpdateSql(string sql)
        {
            return SqlCommand(sql);
        }
        public int UpdateIsTrack(int id, int istrack)
        {
            int i = UpdateSql($"UPDATE camera SET istrack = {istrack} WHERE ID = '{id}'");
            WCamera _camera = CameraManager.GetInstance()[id];
            _camera.IsTrack = istrack;

            return i;
        }
        public int DeleteCamera(int[] id)
        {
            GlobalVariable.TrackStatus = false;
            foreach (var item in id)
            {
                CameraModel cameraModel = GetInfo(item);
                BaseService.LoadUnBindCamera(cameraModel.Ip);
                //MemoryCacheHelper.Remove(cameraModel.Ip);
                _cache.Remove(cameraModel.Ip);
                BaseService.LoadCalibration();
            }
            int i = Delete(id);

            GlobalVariable.TrackStatus = true;
            return i;
        }

        public List<CameraModel> GetAllList()
        {
            return Queryable()
                // .Where(x => x.Status == 1)
                .ToList();
        }

        public List<CameraModel> SelectCameraIds(int BindingAreaId)
        {
            return Queryable()
                .Where(r => r.BindingAreaId == BindingAreaId)
                .ToList();
        }
        public int unUpdateBindCamera(int BindingAreaId)
        {
            var result = Update(w => w.BindingAreaId == BindingAreaId, it => new CameraModel()
            {
                BindingAreaId = -1
            });
            return result;
        }
        public int UpdateBindCamera(DefenceareaModel defence, int id)
        {
            if (defence.CameraIds == null) return 0;

            var result = 0;
            foreach (var item in defence.CameraIds)
            {
                result = Update(w => w.Id == item, it => new CameraModel()
                {
                    BindingAreaId = id
                });

                WCamera cb = CameraManager.GetInstance()[item];
                CameraManager.GetInstance().Remove(cb);

                var CameraList = GetInfo(item);

                //相机添加
                WCamera camera = new WCamera(CameraList);
                CameraManager.GetInstance().Add(camera);
            }
            return result;
        }

        public CameraModel GetCameraByDefenceAreaId(int DefenceAreaId)
        {
            var response = Queryable()
                .Where(x => x.BindingAreaId == DefenceAreaId)
                .First();
            return response;
        }
        public List<CameraModel> GetListCameraByDefenceAreaId(int DefenceAreaId)
        {
            var response = Queryable()
                .Where(x => x.BindingAreaId == DefenceAreaId).ToList();
            return response;
        }

        public string RepetitionJudgment(int[] cameraIds)
        {
            if (cameraIds == null || cameraIds.Length == 0)
                return string.Empty;

            var validCameras = cameraIds
                .Select(GetInfo)
                .Where(camera => camera?.BindingAreaId != -1);

            return string.Join(" ", validCameras.Select(camera =>
                $" | Ip：{camera.Ip} Zone Id：{camera.BindingAreaId} | "));
        }
        public string RepetitionJudgmentEdit(int BindingAreaId, int[] cameraIds)
        {
            if (cameraIds == null || cameraIds.Length == 0)
                return string.Empty;

            var validCameras = cameraIds
                .Select(GetInfo)
                .Where(camera => camera?.BindingAreaId != -1 && camera.BindingAreaId != BindingAreaId);

            return string.Join(" ", validCameras.Select(camera =>
                $" | Ip：{camera.Ip} Zone Id：{camera.BindingAreaId} | "));
        }

        public void GetMinZoomPT(int Id, string Ip)
        {
            //common = MemoryCacheHelper.Get<ONVIF_COMMON_INFO>(Ip);
            common = _cache.Get<ONVIF_COMMON_INFO>(Ip);
            ONVIF_PTZ_STATUS aa = new ONVIF_PTZ_STATUS();
            int ret = onvifsdk.ONVIF_PTZ_GetStatus(2, ref common, ref aa);

            int i = UpdateSql($"UPDATE camera SET minZoomPan = {aa.panPosition},minZoomTilt = {aa.tiltPosition} WHERE ID = '{Id}'");
            WCamera _camera = CameraManager.GetInstance()[Id];
            _camera.minZoomPan = aa.panPosition;
            _camera.minZoomTilt = aa.tiltPosition;
        }

        public void GetMaxZoomPT(int Id, string Ip)
        {
            //common = MemoryCacheHelper.Get<ONVIF_COMMON_INFO>(Ip);
            common = _cache.Get<ONVIF_COMMON_INFO>(Ip);
            ONVIF_PTZ_STATUS aa = new ONVIF_PTZ_STATUS();
            int ret = onvifsdk.ONVIF_PTZ_GetStatus(2, ref common, ref aa);


            int i = UpdateSql($"UPDATE camera SET maxZoomPan = {aa.panPosition},maxZoomTilt = {aa.tiltPosition} WHERE ID = '{Id}'");
            WCamera _camera = CameraManager.GetInstance()[Id];
            _camera.minZoomPan = aa.panPosition;
            _camera.minZoomTilt = aa.tiltPosition;
        }
    }
}
