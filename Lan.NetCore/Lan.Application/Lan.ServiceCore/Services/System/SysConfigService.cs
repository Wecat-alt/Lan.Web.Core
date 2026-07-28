using Infrastructure;
using Lan.Dto;
using Lan.Model;
using Lan.Repository;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.Public;
using Model;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.ServiceCore.Services
{
    [AppService(ServiceType = typeof(ISysConfigService), ServiceLifetime = LifeTime.Transient)]
    public class SysConfigService : Repository<SysConfig>, ISysConfigService
    {
        public PagedInfo<SysConfigDto> GetList(SysConfigQueryDto parm)
        {
            var exp = Expressionable.Create<AlarmModel>();
            //exp.AndIF(!string.IsNullOrEmpty(parm.ip), u => u.CameraIp.Contains(parm.ip));
            //exp.AndIF(!string.IsNullOrEmpty(parm.AreaId.ToString()), u => u.AreaId == parm.AreaId);
            //if (parm.defenceareaId != 0)
            //{
            //    exp.AndIF(!string.IsNullOrEmpty(parm.defenceareaId.ToString()), u => u.BindingAreaId == parm.defenceareaId);
            //}

            var response = Queryable()

            .OrderBy(u => u.ConfigId, OrderByType.Asc)
            .ToPage<SysConfig, SysConfigDto>(parm);

            return response;
        }
        public SysConfig GetSysConfigByKey(string key)
        {
            return Queryable().First(f => f.ConfigKey == key);
        }

        public SysConfig GetSysConfig(int Id)
        {
            var response = Queryable()
                .Where(x => x.ConfigId == Id)
                .First();
            return response;
        }

        public SysConfig AddSysConfig(SysConfig model)
        {
            return Insertable(model).ExecuteReturnEntity();
        }

        public int UpdateSysConfig(SysConfig model)
        {
            var result = Update(model, true);
            // 同步刷新内存中的全局变量
            if (model.ConfigKey == "maxAlarmTime" && int.TryParse(model.ConfigValue, out int maxAlarm))
            {
                GlobalVariable.maxAlarmTime = maxAlarm;
            }
            else if (model.ConfigKey == "radarAlarmOvertime" && int.TryParse(model.ConfigValue, out int overtime))
            {
                GlobalVariable.radarAlarmOvertime = overtime;
            }
            return result;
        }
        public int DeleteSysConfig(object id)
        {
            return Delete(id);
        }
    }
}
