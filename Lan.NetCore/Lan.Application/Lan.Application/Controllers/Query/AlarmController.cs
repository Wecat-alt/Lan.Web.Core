using Lan.Dto;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace Lan.Application.Controllers.Query
{
    [Route("api/alarm")]
    [ApiController]
    public class AlarmController : BaseController
    {
        private readonly IAlarmService _alarmService;
        private readonly ILogger<AlarmController> _logger; // 方案一
        public AlarmController(ILogger<AlarmController> logger, IAlarmService alarmService)//, IHubContext<MessageHub> hubContext
        {
            _logger = logger;
            _alarmService = alarmService;
        }
        [HttpGet("list")]
        public IActionResult QueryCamera([FromQuery] AlarmQueryDto parm)
        {
            var response = _alarmService.GetList(parm);
            return Message(response);
        }

        [HttpGet("list1")]
        public IActionResult UpdateAllCamera([FromQuery] AlarmQueryDto parm)
        {
            var response = _alarmService.UpdateAllAlarm(parm);
            return Message(response);
        }

        [HttpGet("listref")]
        public IActionResult QueryCameraRef([FromQuery] AlarmQueryDto parm)
        {
            var response = _alarmService.GetListRef(parm);
            return Message(response);
        }

        [HttpGet("{Id}")]
        public IActionResult GetAlarm(int Id)
        {
            var response = _alarmService.GetInfo(Id);
            var info = response.Adapt<AlarmDto>();
            return Message(info);
        }

        [HttpDelete("update/{ids}")]
        public IActionResult UpdateAlarm([FromRoute] string ids)
        {
            var idArr = Lan.Tools.Tools.SplitAndConvert<int>(ids);

            var response = _alarmService.UpdateAlarm(idArr);

            return ToResponse(response);
        }
    }
}
