using Lan.Dto;
using Lan.Model;
using Lan.Repository;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.ServiceCore.IService
{
    public interface ISysConfigService 
    {
        PagedInfo<SysConfigDto> GetList(SysConfigQueryDto parm);
        SysConfig GetSysConfigByKey(string key);
        SysConfig GetSysConfig(int Id);
        SysConfig AddSysConfig(SysConfig parm);
        int UpdateSysConfig(SysConfig parm);
        int DeleteSysConfig(object id);
    }
}
