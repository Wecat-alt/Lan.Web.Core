using Lan.Infrastructure.CameraOnvif;
using Lan.ServiceCore.Services;
using MemoryCache.Core;
using Microsoft.Extensions.DependencyInjection;
using Model;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using static Azure.Core.HttpHeader;

namespace Lan.ServiceCore.Onvif
{
    public interface IOnvifManage
    {
        void Init();
    }

    public class OnvifManage : IOnvifManage
    {
        private ONVIF_MANAGEMENT_CAPABILITIES capabilities = new ONVIF_MANAGEMENT_CAPABILITIES();

        private readonly IMemoryCacheService _cache;
        public OnvifManage(IMemoryCacheService cache) { _cache = cache; }

        public void Init()
        {
            List<CameraModel> listCamera = new CameraService().GetAllList();

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
                            //MemoryCacheHelper.Set(v.Ip, oNVIF_COMMON_INFO);
                            _cache.Set(v.Ip, oNVIF_COMMON_INFO);
                            //var session = _cache.Get<ONVIF_COMMON_INFO>(v.Ip);
                        }
                    }
                }
            }
        }
    }
}
