using Lan.Dto;
using Lan.Model;
using Lan.Model.Vo;
using Lan.ServiceCore.TargetCollection;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.ServiceCore.IService
{
    public interface IDefenceareaService
    {
        public DefenceareaModel InsertUser(DefenceareaModel sysUser);
        List<DefenceareaModel> GetList(DefenceareaQueryDto parm);
        DefenceareaModel GetInfo(int Id);
        int AddDefencearea(DefenceareaModel parm);
        bool GetInfoByName(string Name);
        bool GetInfoByName(string Name, int id);
        int EnableDefencearea(int status);
        int UpdateDefencearea(DefenceareaModel parm);
        int DeleteDefencearea(int[] id);
        List<DefenceareaModel> GetAllList();

        List<TreeSelectVo> BuildDeptTreeSelect(List<DefenceareaDtoParent> depts);
    }
}
