using Lan.ServiceCore.Public;
using Microsoft.AspNetCore.Mvc;

namespace Lan.Application.Controllers.Base
{
    /// <summary>
    /// 前端 config.js 配置更新接口
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
        /// 更新前端 config.js 中的 IP 地址（ip 必填）
        /// </summary>
        [HttpGet]
        public IActionResult UpdateConfig([FromQuery] string? ip = null)
        {
            var err = ConfigJsUpdater.ValidateIp(ip);
            if (err != null)
                return Message(err);

            var result = _configJsUpdater.UpdateConfigJs(ip!);
            return Message(result);
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
