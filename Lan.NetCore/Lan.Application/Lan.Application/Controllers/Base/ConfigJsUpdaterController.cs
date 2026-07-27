using Lan.ServiceCore.Public;
using Microsoft.AspNetCore.Mvc;

namespace Lan.Application.Controllers.Base
{
    /// <summary>
    /// 前端 config.js 和 appsettings.json 配置同步更新接口
    /// </summary>
    [Route("api/ansyc")]
    [ApiController]
    public class ConfigJsUpdaterController : BaseController
    {
        private readonly ConfigJsUpdater _configJsUpdater;

        public ConfigJsUpdaterController(ConfigJsUpdater configJsUpdater)
        {
            _configJsUpdater = configJsUpdater;
        }

        /// <summary>
        /// 同步更新 config.js 和 appsettings.json 中的 IP 地址（ip 必填）
        /// </summary>
        [HttpGet]
        public IActionResult UpdateConfig([FromQuery] string? ip = null)
        {
            var err = ConfigJsUpdater.ValidateIp(ip);
            if (err != null)
                return Message(err);

            var results = new List<string>();

            // 1. 更新 config.js
            results.Add(_configJsUpdater.UpdateConfigJs(ip!));

            // 2. 更新 appsettings.json 中的 CorsUrls
            var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            results.Add(_configJsUpdater.UpdateAppSettings(ip!, appSettingsPath));

            // 3. 回收应用池使 CORS 配置生效
            var webConfigPath = Path.Combine(AppContext.BaseDirectory, "web.config");
            results.Add(_configJsUpdater.RecycleAppPool(webConfigPath));

            return Message(string.Join(" | ", results));
        }

        /// <summary>
        /// 获取本机所有可用的 IPv4 地址
        /// </summary>
        [HttpGet("ip")]
        public IActionResult GetLocalIp()
        {
            var ips = ConfigJsUpdater.GetAllLocalIPv4();
            return Message(ips);
        }
    }
}
