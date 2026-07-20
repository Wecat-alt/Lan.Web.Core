using Lan.ServiceCore.Onvif;
using Lan.ServiceCore.Public;
using Lan.ServiceCore.Services;
using Lan.ServiceCore.TargetCollection;
using Lan.ServiceCore.WebScoket;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.ServiceCore.IService.Base
{
    public interface IBaseService
    {
        void LoadCalibration();
        void LoadDefenceAreaAdd(int id);
        void LoadDefenceAreaUpdate(DefenceareaModel model);
        void LoadDefenceAreaUpdate(int status);
        void LoadDefenceAreaDelete(int _id);
        void LoadRadarAdd(int id);
        void LoadDeleteRadar(string ip);
        void LoadCameraAdd(int _id);
        void LoadCameraUpdate(int _id);
        void LoadUnBindCamera(string ip);
        void LoadUnBindCamera(string status, string ip);
    }
}
