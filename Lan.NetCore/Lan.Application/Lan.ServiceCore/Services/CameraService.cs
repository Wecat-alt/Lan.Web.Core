using Infrastructure;
using Lan.Dto;
using Lan.Infrastructure.Cache;
using Lan.Infrastructure.CameraOnvif;
using Lan.Model;
using Lan.Repository;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.Onvif;
using Lan.ServiceCore.Public;
using Lan.ServiceCore.Services.Base;
using Lan.ServiceCore.TargetCollection;
using Lan.ServiceCore.WebScoket;
using Model;
using SqlSugar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;

namespace Lan.ServiceCore.Services
{
    [AppService(ServiceType = typeof(ICameraService), ServiceLifetime = LifeTime.Singleton)]
    public class CameraService : Repository<CameraModel>, ICameraService
    {
        ONVIF_COMMON_INFO common = new ONVIF_COMMON_INFO();
        public CameraService()
        {

        }

        public List<CameraModel> GetList(CameraQueryDto parm)
        {
            var predicate = QueryExp(parm);

            var exp = Expressionable.Create<CameraModel>();
            exp.AndIF(!string.IsNullOrEmpty(parm.ip), u => u.Ip.Contains(parm.ip));
            exp.AndIF(!string.IsNullOrEmpty(parm.defenceareaId.ToString()), u => u.BindingAreaId == parm.defenceareaId);

            var response = Queryable()
                .Where(exp.ToExpression()).ToList();

            return response;
        }

        private static Expressionable<CameraModel> QueryExp(CameraQueryDto parm)
        {
            var predicate = Expressionable.Create<CameraModel>();

            return predicate;
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
                MemoryCacheHelper.Remove(cameraModel.Ip);
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
            common = MemoryCacheHelper.Get<ONVIF_COMMON_INFO>(Ip);

            ONVIF_PTZ_STATUS aa = new ONVIF_PTZ_STATUS();
            int ret = onvifsdk.ONVIF_PTZ_GetStatus(2, ref common, ref aa);

            int i = UpdateSql($"UPDATE camera SET minZoomPan = {aa.panPosition},minZoomTilt = {aa.tiltPosition} WHERE ID = '{Id}'");
            WCamera _camera = CameraManager.GetInstance()[Id];
            _camera.minZoomPan = aa.panPosition;
            _camera.minZoomTilt = aa.tiltPosition;
        }

        public void GetMaxZoomPT(int Id, string Ip)
        {
            common = MemoryCacheHelper.Get<ONVIF_COMMON_INFO>(Ip);
            ONVIF_PTZ_STATUS aa = new ONVIF_PTZ_STATUS();
            int ret = onvifsdk.ONVIF_PTZ_GetStatus(2, ref common, ref aa);


            int i = UpdateSql($"UPDATE camera SET maxZoomPan = {aa.panPosition},maxZoomTilt = {aa.tiltPosition} WHERE ID = '{Id}'");
            WCamera _camera = CameraManager.GetInstance()[Id];
            _camera.minZoomPan = aa.panPosition;
            _camera.minZoomTilt = aa.tiltPosition;
        }
    }
}
