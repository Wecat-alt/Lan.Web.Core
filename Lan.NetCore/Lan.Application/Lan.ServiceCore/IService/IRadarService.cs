using Lan.Dto;
using Model;

namespace Lan.ServiceCore.IService
{
    /// <summary>
    /// service接口
    /// </summary>
    public interface IRadarService
    {
        List<RadarModel> GetList(RadarQueryDto parm);
        List<RadarModel> GetListALL();
        List<RadarModel> GetListByAreaId(int Id);
        RadarModel GetInfo(int Id);
        bool GetInfoByIp(string Ip);
        RadarModel AddRadar(RadarModel parm);
        int UpdateRadar(RadarModel parm);
        int UpdateRadarLatLng(string Ip, string Latitude, string Longitude);
        int DeleteRadar(int[] id);
        List<RadarModel> GetAllList();
        List<RadarModel> SelectRadarIds(int BindingAreaId);
        int unUpdateBindRadar(int BindingAreaId);
        int UpdateBindRadar(DefenceareaModel defence, int id);
        string RepetitionJudgment(int[] CameraIds);
        string RepetitionJudgmentEdit(int BindingAreaId, int[] RadarIds);
    }
}
