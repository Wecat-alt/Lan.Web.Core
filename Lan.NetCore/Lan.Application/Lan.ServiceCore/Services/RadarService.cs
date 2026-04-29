using Infrastructure;
using Lan.Dto;
using Lan.Infrastructure;
using Lan.Model;
using Lan.Repository;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.Public;
using Lan.ServiceCore.Services.Base;
using Lan.ServiceCore.TargetCollection;
using Lan.ServiceCore.WebScoket;
using Model;
using NetTopologySuite.Index.HPRtree;
using SqlSugar;
using WebSocketSharp;

namespace Lan.ServiceCore.Services
{
    /// <summary>
    /// Service业务层处理
    /// </summary>
    [AppService(ServiceType = typeof(IRadarService), ServiceLifetime = LifeTime.Singleton)]
    public class RadarService : Repository<RadarModel>, IRadarService
    {
        public List<RadarModel> GetList(RadarQueryDto parm)
        {
            var predicate = QueryExp(parm);

            var exp = Expressionable.Create<RadarModel>();
            exp.AndIF(!string.IsNullOrEmpty(parm.ip), u => u.Ip.Contains(parm.ip));

            var response = Queryable()
                .LeftJoin<DefenceareaModel>((u, c) => u.BindingAreaId == c.Id)  // 修正：包含所有已连接的表
                .LeftJoin<CameraModel>((u, c, d) => c.Id == d.BindingAreaId)  // 修正：包含所有已连接的表
                .Where(exp.ToExpression())
                .Select((u, c, d) => new RadarModel
                {
                    Id = u.Id,
                    BindingAreaId = u.BindingAreaId,
                    Ip = u.Ip,
                    Port = u.Port,
                    Status = u.Status,
                    Latitude = u.Latitude,
                    Longitude = u.Longitude,
                    NorthDeviationAngle = u.NorthDeviationAngle,
                    DefenceRadius = u.DefenceRadius,
                    DefenceAngle = u.DefenceAngle,
                    RadarType = u.RadarType,

                    DefenceEnable = c.DefenceEnable,
                    Online = false,
                    CameraIp = d.Ip,
                    Username = d.Username,
                    Password = d.Password,
                    CameraURL = d.CameraURL,
                })
                .ToList();
            var radarManager = RadarManager.GetInstance();
            foreach (var radar in response)
            {
                var radarStatus = radarManager[radar.Ip];
                radar.Online = radarStatus?.Online ?? false;
            }
            return response;
        }

        private static Expressionable<RadarModel> QueryExp(RadarQueryDto parm)
        {
            var predicate = Expressionable.Create<RadarModel>();

            return predicate;
        }

        public List<RadarModel> GetListALL() => Queryable()
                .LeftJoin<DefenceareaModel>((u, c) => u.BindingAreaId == c.Id)  // 修正：包含所有已连接的表
                .LeftJoin<CameraModel>((u, c, d) => c.Id == d.BindingAreaId)  // 修正：包含所有已连接的表
                .Select((u, c, d) => new RadarModel
                {
                    Id = u.Id,
                    BindingAreaId = u.BindingAreaId,
                    Ip = u.Ip,
                    Port = u.Port,
                    Status = u.Status,
                    Latitude = u.Latitude,
                    Longitude = u.Longitude,
                    NorthDeviationAngle = u.NorthDeviationAngle,
                    DefenceRadius = u.DefenceRadius,
                    DefenceAngle = u.DefenceAngle,
                    RadarType = u.RadarType,

                    DefenceEnable = c.DefenceEnable,
                    Online = false,
                    CameraIp = d.Ip,
                    Username = d.Username,
                    Password = d.Password,
                    CameraURL = d.CameraURL,
                }).ToList();

        public RadarModel GetInfo(int Id) => Queryable().Where(x => x.Id == Id).First();
        public RadarModel GetInfo(string Ip) => Queryable().Where(x => x.Ip == Ip).First();
        public List<RadarModel> GetListByAreaId(int Id)
        {
            var response = Queryable()
                .Where(x => x.BindingAreaId == Id).ToList();
            return response;
        }
        public bool GetInfoByIp(string Ip)
        {
            bool bl = Queryable().Any(x => x.Ip == Ip);
            return bl;
        }

        public RadarModel AddRadar(RadarModel model)
        {
            GlobalVariable.TrackStatus = false;
            var res = Insertable(model).ExecuteReturnEntity();
            BaseService.LoadRadarAdd(res.Id);
            GlobalVariable.TrackStatus = true;
            return res;
        }

        public int UpdateRadar(RadarModel model)
        {
            GlobalVariable.TrackStatus = false;
            int i = Update(model, true);

            WRadar wRadar = RadarManager.GetInstance()[model.Ip];
            RadarManager.GetInstance().Remove(wRadar);

            RadarManager.GetInstance().Add(model);
            GlobalVariable.TrackStatus = true;
            return i;
        }

        public int UpdateRadarLatLng(string Ip, string Latitude, string Longitude)
        {
            GlobalVariable.TrackStatus = false;
            int i = UpdateSql($"UPDATE radar SET Latitude = {Latitude},Longitude={Longitude} WHERE Ip = '{Ip}'");
            WRadar wRadar = RadarManager.GetInstance()[Ip];
            RadarManager.GetInstance().Remove(wRadar);

            RadarModel radarModel = GetInfo(Ip);

            RadarManager.GetInstance().Add(radarModel);
            GlobalVariable.TrackStatus = true;
            return i;
        }

        public int UpdateSql(string sql)
        {
            return SqlCommand(sql);
        }
        public int DeleteRadar(int[] id)
        {
            GlobalVariable.TrackStatus = false;
            foreach (var item in id)
            {
                RadarModel radarModel = GetInfo(item);
                BaseService.LoadDeleteRadar(radarModel.Ip);
            }
            int i = Delete(id);
            GlobalVariable.TrackStatus = true;
            return i;
        }

        public List<RadarModel> GetAllList()
        {
            return Queryable()
                .Where(x => x.Status == 1)
                .ToList();
        }

        public List<RadarModel> SelectRadarIds(int BindingAreaId)
        {
            return Queryable()
                .Where(r => r.BindingAreaId == BindingAreaId)
                //.OrderBy(role => role.RoleSort)
                .ToList();
        }

        public int unUpdateBindRadar(int BindingAreaId)
        {
            var result = Update(w => w.BindingAreaId == BindingAreaId, it => new RadarModel()
            {
                BindingAreaId = -1
            });
            return result;
        }

        public int UpdateBindRadar(DefenceareaModel defence, int id)
        {
            if (defence.RadarIds == null) return 0;

            var result = 0;
            foreach (var item in defence.RadarIds)
            {
                result = Update(w => w.Id == item, it => new RadarModel()
                {
                    BindingAreaId = id
                });


                var RadarList = GetInfo(item);

                WRadar wRadar = RadarManager.GetInstance()[RadarList.Ip];
                RadarManager.GetInstance().Remove(wRadar);

                RadarManager.GetInstance().Add(RadarList);
            }
            return result;
        }

        public string RepetitionJudgment(int[] radarIds)
        {
            if (radarIds == null || radarIds.Length == 0)
                return string.Empty;

            var validCameras = radarIds
                .Select(GetInfo)
                .Where(camera => camera?.BindingAreaId != -1);

            return string.Join(" ", validCameras.Select(camera =>
                $" | Ip：{camera.Ip} Zone Id：{camera.BindingAreaId} | "));
        }
        public string RepetitionJudgmentEdit(int BindingAreaId, int[] radarIds)
        {
            if (radarIds == null || radarIds.Length == 0)
                return string.Empty;

            var validRadars = radarIds
                .Select(GetInfo)
                .Where(radar => radar?.BindingAreaId != -1 && radar.BindingAreaId != BindingAreaId);

            return string.Join(" ", validRadars.Select(radar =>
                $" | Ip：{radar.Ip} Zone Id：{radar.BindingAreaId} | "));
        }
    }
}