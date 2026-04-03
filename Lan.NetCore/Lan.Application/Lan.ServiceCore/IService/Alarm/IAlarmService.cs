using Lan.Dto;
using Lan.Model;
using Model;

namespace Lan.ServiceCore.IService
{
    public interface IAlarmService
    {
        PagedInfo<AlarmDto> GetList(AlarmQueryDto parm);
        List<AlarmModel> GetListRef(AlarmQueryDto parm);
        AlarmModel AddAlarm(AlarmModel alarm);
        int UpdateAlarm(int[] ids);
        int UpdateAllAlarm(AlarmQueryDto parm);
        AlarmModel GetInfo(int Id);
    }
}
