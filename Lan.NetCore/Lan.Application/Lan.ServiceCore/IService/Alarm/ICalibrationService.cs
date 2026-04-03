using Model;

namespace Lan.ServiceCore.IService
{
    public interface ICalibrationService
    {
        string AddCalibration(Calibration model);
        void cameraPTZ(string _CameraPTZ, string ip, string speed);
        void PtzStop(string ip);
        Calibration GetInfo(string CameraIp, int DefenceareaId);
        int UpdateCalibration(string CameraIp, int DefenceareaId, float CalibrationDistance, float CameraPointX, float CameraPointY, float CamerarPointAngle);
    }
}
