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
            CameraService cameraService = new();
            var ds = cameraService.GetAllList();

            if (ds.Count == 0)
                return [];

            List<WCamera> list = new(ds.Count);
            foreach (CameraModel dtRow in ds)
            {
                try
                {
                    list.Add(new WCamera(dtRow));
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine("从数据库加载相机失败：\r\n" + ex.ToString());
                }
            }

            return [.. list];
        }
    }
}
