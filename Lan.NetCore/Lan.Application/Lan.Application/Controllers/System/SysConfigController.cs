using Infrastructure.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace Lan.Application.Controllers.System
{
    [Route("api/config")]
    [ApiController]
    public class SysConfigController : BaseController
    {
        private readonly ISysConfigService _SysConfigService;

        public SysConfigController(ISysConfigService SysConfigService)
        {
            _SysConfigService = SysConfigService;
        }

        [HttpGet("list")]
        public IActionResult QuerySysConfig([FromQuery] SysConfigQueryDto parm)
        {
            //var predicate = Expressionable.Create<SysConfig>();

            //predicate = predicate.AndIF(!parm.ConfigType.IsEmpty(), m => m.ConfigType == parm.ConfigType);
            //predicate = predicate.AndIF(!parm.ConfigName.IsEmpty(), m => m.ConfigName.Contains(parm.ConfigType));
            //predicate = predicate.AndIF(!parm.ConfigKey.IsEmpty(), m => m.ConfigKey.Contains(parm.ConfigKey));
            //predicate = predicate.AndIF(!parm.BeginTime.IsEmpty(), m => m.Create_time >= parm.BeginTime);
            //predicate = predicate.AndIF(!parm.BeginTime.IsEmpty(), m => m.Create_time <= parm.EndTime);

            //var response = _SysConfigService.GetPages(predicate.ToExpression(), parm);

            //return Message(response);

            var response = _SysConfigService.GetList(parm);
            return Message(response);
        }

        [HttpGet("configKey/{configKey}")]
        [AllowAnonymous]
        public IActionResult GetConfigKey(string configKey)
        {
            var response = _SysConfigService.GetSysConfigByKey(configKey);

            return Message(response?.ConfigValue);
        }


        [HttpGet("{ConfigId}")]
        public IActionResult GetSysConfig(int ConfigId)
        {
            var response = _SysConfigService.GetSysConfig(ConfigId);

            return Message(response);
        }

        [HttpPost]
        public IActionResult AddSysConfig([FromBody] SysConfigDto parm)
        {
            var modal = parm.Adapt<SysConfig>().ToCreate(HttpContext);

            var response = _SysConfigService.AddSysConfig(modal);

            return Message(response);
        }

        [HttpPut]
        public IActionResult UpdateSysConfig([FromBody] SysConfigDto parm)
        {
            var modal = parm.Adapt<SysConfig>().ToUpdate(HttpContext);
            var response = _SysConfigService.UpdateSysConfig(modal);

            return ToResponse(response);
        }

        [HttpDelete("{ids}")]
        public IActionResult DeleteSysConfig(string ids)
        {
            var idArr = Lan.Tools.Tools.SplitAndConvert<int>(ids);

            return ToResponse(_SysConfigService.DeleteSysConfig(idArr));
        }
    }
}
