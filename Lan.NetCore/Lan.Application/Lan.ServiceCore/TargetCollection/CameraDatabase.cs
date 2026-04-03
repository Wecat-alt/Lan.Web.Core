using Lan.ServiceCore.Services;
using Lan.ServiceCore.TargetCollection;
using Model;
using System.Data;

namespace Lan.Database
{
    public class CameraDatabase 
    {
        public static WCamera[] GetCameras()
        {
            CameraService cameraService = new CameraService();
            var ds = cameraService.GetAllList();

            if (ds.Count == 0)
                return new WCamera[0];

            List<WCamera> list = new List<WCamera>(ds.Count);
            foreach (CameraModel dtRow in ds)
            {
                try
                {
                    WCamera camera = new WCamera(dtRow);
                    list.Add(camera);
                }
                catch (System.Exception ex)
                {
                    //Log.Error("从数据库加载相机失败：\r\n" + ex.ToString());
                    Console.WriteLine("从数据库加载相机失败：\r\n" + ex.ToString());
                }
            }

            return list.ToArray();
        }
    }
}
