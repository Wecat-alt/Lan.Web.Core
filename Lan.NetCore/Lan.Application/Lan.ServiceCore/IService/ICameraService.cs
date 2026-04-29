using Lan.Dto;
using Lan.ServiceCore.TargetCollection;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.ServiceCore.IService
{
    public interface ICameraService
    {
        List<CameraModel> GetList(CameraQueryDto parm);
        Task<List<CameraModel>> GetListPreviewAsync();
        CameraModel GetInfo(int Id);
        CameraModel GetInfo(string Ip);
        bool GetInfoByIp(string Ip);
        int AddCamera(CameraModel parm);
        int UpdateCamera(CameraModel parm);
        int DeleteCamera(int[] id);
        List<CameraModel> GetAllList();
        List<CameraModel> SelectCameraIds(int BindingAreaId);
        int unUpdateBindCamera(int BindingAreaId);
        int UpdateBindCamera(DefenceareaModel defence, int id);
        CameraModel GetCameraByDefenceAreaId(int DefenceAreaId);
        List<CameraModel> GetListCameraByDefenceAreaId(int DefenceAreaId);
        int UpdateIsTrack(int id, int istrack);
        string RepetitionJudgment(int[] CameraIds);
        string RepetitionJudgmentEdit(int BindingAreaId, int[] cameraIds);
        void GetMinZoomPT(int Id, string Ip);
        void GetMaxZoomPT(int Id, string Ip);

    }
}
