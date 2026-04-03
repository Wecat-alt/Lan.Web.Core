using Lan.Infrastructure.Cache;
using Lan.Infrastructure.CameraOnvif;
using Lan.ServiceCore.Services;
using Model;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using static Azure.Core.HttpHeader;

namespace Lan.ServiceCore.Onvif
{
    public class OnvifManage
    {
        static ONVIF_MANAGEMENT_CAPABILITIES capabilities = new ONVIF_MANAGEMENT_CAPABILITIES();

        public static void Init()
        {
            CameraService cameraService = new CameraService();
            List<CameraModel> listCamera = cameraService.GetAllList();

            if (listCamera is { Count: > 0 })
            {
                foreach (var v in listCamera)
                {
                    if (!string.IsNullOrEmpty(v.CameraURL))
                    {
                        Console.WriteLine("ONVIF_MAGEMENT_GetCapabilitiesEx..");
                        int ret = onvifsdk.ONVIF_MAGEMENT_GetCapabilitiesEx(20, v.Ip, 80, v.Username, v.Password, ref capabilities);
                        Console.WriteLine("ONVIF_MAGEMENT_GetCapabilitiesEx result: " + ret);
                        if (ret == 0)
                        {
                            ONVIF_COMMON_INFO oNVIF_COMMON_INFO = new ONVIF_COMMON_INFO()
                            {
                                username = v.Username,
                                password = v.Password,
                                onvifUrls = capabilities.onvifUrls,
                                sourceToken = v.SourceToken,
                            };

                            //ONVIF_DEVICE_INFO oNVIF_DEVICE_INFO = new ONVIF_DEVICE_INFO();
                            //ret = onvifsdk.ONVIF_MAGEMENT_GetDeviceInformation(2000, ref oNVIF_COMMON_INFO, ref oNVIF_DEVICE_INFO);
                            //string a = Encoding.Default.GetString(oNVIF_DEVICE_INFO.firmwareVersion).TrimEnd('\0');
                            //string b = Encoding.Default.GetString(oNVIF_DEVICE_INFO.hardwareId).TrimEnd('\0'); ;
                            //string c = Encoding.Default.GetString(oNVIF_DEVICE_INFO.manufacturer).TrimEnd('\0');
                            //string d = Encoding.Default.GetString(oNVIF_DEVICE_INFO.model).TrimEnd('\0');
                            //string e = Encoding.Default.GetString(oNVIF_DEVICE_INFO.serialNumber).TrimEnd('\0');
                            //string aa = ret.ToString();

                            MemoryCacheHelper.Set(v.Ip, oNVIF_COMMON_INFO);
                            //var session = MemoryCacheHelper.Get<ONVIF_COMMON_INFO>(v.Ip);
                        }
                    }
                }
            }
        }

    }
}
