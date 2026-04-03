using Infrastructure;
using Lan.Dto;
using Lan.Model;
using Lan.Repository;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.Public;
using Model;
using NetTopologySuite.Algorithm;
using SqlSugar;
using System.Linq.Expressions;

namespace Lan.ServiceCore.Services
{
    [AppService(ServiceType = typeof(IAlarmService), ServiceLifetime = LifeTime.Transient)]
    public class AlarmService : Repository<AlarmModel>, IAlarmService
    {
        public AlarmService() { }


        public PagedInfo<AlarmDto> GetList(AlarmQueryDto parm)
        {
            var exp = Expressionable.Create<AlarmModel>();
            exp.AndIF(!string.IsNullOrEmpty(parm.ip), u => u.CameraIp.Contains(parm.ip));
            exp.AndIF(!string.IsNullOrEmpty(parm.AreaId.ToString()), u => u.AreaId == parm.AreaId);

            exp.AndIF(!string.IsNullOrEmpty(parm.StartTime), u => u.DateTime >= DateTime.Parse(parm.StartTime + " 00:00:00"));
            exp.AndIF(!string.IsNullOrEmpty(parm.EndTime), u => u.DateTime <= DateTime.Parse(parm.EndTime + " 23:59:59"));

            var response = Queryable()
            .Where(exp.ToExpression())
            .Where(u => SqlFunc.Subqueryable<TrackInfo>()
                .Where(t => t.AreaId == u.AreaId)
                .Where(t => t.UpdateTime >= u.DateTime)
                .Where(t => t.UpdateTime <= SqlFunc.DateAdd(u.DateTime, 15, DateType.Second))
                .Any())
            .Select((u) => new AlarmModel
            {

                Id = u.Id,
                CameraIp = u.CameraIp,
                AreaId = u.AreaId,
                AreaName = u.AreaName,
                DateTime = u.DateTime,
                DealWith = u.DealWith,
                VideoName = u.VideoName.Replace(GlobalVariable.FilePath.Replace("\\", "/"), GlobalVariable.recordservicehost),
                Level = u.Level,
                Latitude = u.Latitude,
                Longitude = u.Longitude
            })
            .OrderBy(u => u.Id, OrderByType.Desc)
            .ToPage<AlarmModel, AlarmDto>(parm);
            
            return response;
        }
        public List<AlarmModel> GetListRef(AlarmQueryDto parm)
        {
            var exp = Expressionable.Create<AlarmModel>();
            var response = Queryable().Take(10).OrderBy(u => u.Id, OrderByType.Desc).ToList();
            return response;
        }

        public AlarmModel AddAlarm(AlarmModel alarm)
        {
            return Insertable(alarm).ExecuteReturnEntity();
        }

        public async Task<int> AddAlarmAndReturnIdAsync(AlarmModel alarm)
        {
            try
            {
                // 使用SqlSugar插入并返回ID
                var id = await Insertable(alarm).ExecuteReturnIdentityAsync();
                return (int)id;
            }
            catch (Exception ex)
            {
                // 日志记录
                throw;
            }
        }

        public int GatMaxAlarmId(int AreaId)
        {
            //db.Queryable<Order>().Max(it => it.Id);//同步 
            //db.Queryable<Order>().MaxAsync(it => it.Id);//异步
            Expression<Func<AlarmModel, bool>> expression = x => x.AreaId == AreaId;
            var maxID = 0;//Context.Queryable<AlarmModel>().Where(expression).Max(x => x.Id);
            return maxID;
        }

        public AlarmModel GetInfo(int Id)
        {
            var response = Queryable()
                .Where(x => x.Id == Id)
                .First();
            return response;
        }

        public int GetInfo(int AreaId, string DealWith)
        {
            var response = Queryable()
                .Where(it => it.AreaId == AreaId && it.DealWith == DealWith).Max(it => it.Id);

            return response;
        }
        public int GetInfos(int AreaId)
        {
            var response = Queryable()
                .Where(x => x.AreaId == AreaId).Max(p => p.Id);

            return response;
        }
        public int UpdateAlarm(int[] ids)
        {
            var result = -1;
            foreach (var id in ids)
            {
                result = Update(w => w.Id == id, it => new AlarmModel()
                {
                    DealWith = "已处理"
                });
            }
            return result;
        }
        public int UpdateAllAlarm(AlarmQueryDto parm)
        {
            var exp = Expressionable.Create<AlarmModel>();
            exp.AndIF(!string.IsNullOrEmpty(parm.ip), u => u.CameraIp.Contains(parm.ip));
            exp.AndIF(!string.IsNullOrEmpty(parm.AreaId.ToString()), u => u.AreaId == parm.AreaId);
            exp.AndIF(!string.IsNullOrEmpty(parm.StartTime), u => u.DateTime >= DateTime.Parse(parm.StartTime + " 00:00:00"));
            exp.AndIF(!string.IsNullOrEmpty(parm.EndTime), u => u.DateTime <= DateTime.Parse(parm.EndTime + " 23:59:59"));

            var result = Update(exp.ToExpression(), it => new AlarmModel()
            {
                DealWith = "已处理"
            });

            return result;
        }
        public int DeleteAlarm(int id)
        {
            return DeleteTable(id); ;
        }
    }
}
