using Lan.Infrastructure.CameraOnvif;
using Lan.ServiceCore.IService;
using MemoryCache.Core;
using Model;

namespace Lan.ServiceCore.Onvif
{
    /// <summary>
    /// ONVIF_COMMON_INFO 缓存读取辅助类。
    /// 统一处理"缓存未命中 → 从数据库重新加载 → 调用 ONVIF SDK → 回填缓存"的降级逻辑，
    /// 避免缓存崩了之后 ONVIF 操作直接失败。
    /// </summary>
    public static class OnvifCacheHelper
    {
        /// <summary>
        /// 从缓存读取 ONVIF_COMMON_INFO，缓存未命中时自动从数据库 + ONVIF SDK 重建并回填。
        /// 仍可能返回 default（相机不在数据库或 ONVIF 通信失败），调用方需判空。
        /// </summary>
        public static ONVIF_COMMON_INFO GetOrRefresh(string ip, IMemoryCacheService? cache)
        {
            if (string.IsNullOrEmpty(ip))
                return default;

            // 先读缓存
            if (cache != null)
            {
                var cached = cache.Get<ONVIF_COMMON_INFO>(ip);
                // 结构体需判字段：username 为空一定不是有效缓存
                if (!string.IsNullOrEmpty(cached.username))
                    return cached;
            }

            // 缓存未命中 → 从数据库重建
            return RefreshFromDb(ip, cache);
        }

        private static ONVIF_COMMON_INFO RefreshFromDb(string ip, IMemoryCacheService? cache)
        {
            try
            {
                var cameraService = App.GetService<ICameraService>();
                var camera = cameraService?.GetInfo(ip);
                if (camera == null || string.IsNullOrEmpty(camera.CameraURL))
                    return default;

                var capabilities = new ONVIF_MANAGEMENT_CAPABILITIES();
                int ret = onvifsdk.ONVIF_MAGEMENT_GetCapabilitiesEx(
                    20, camera.Ip, 80, camera.Username, camera.Password, ref capabilities);

                if (ret != 0)
                    return default;

                var common = new ONVIF_COMMON_INFO
                {
                    username = camera.Username,
                    password = camera.Password,
                    onvifUrls = capabilities.onvifUrls,
                    sourceToken = camera.SourceToken,
                };

                cache?.Set(ip, common);
                return common;
            }
            catch
            {
                // 降级失败，返回 default，由调用方决定如何处理
                return default;
            }
        }
    }
}
