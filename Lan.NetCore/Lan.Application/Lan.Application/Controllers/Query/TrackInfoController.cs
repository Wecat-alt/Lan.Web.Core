using Lan.Model.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lan.Application.Controllers.Query
{
    [Route("api/trackinfo")]
    [ApiController]
    public class TrackInfoController : BaseController
    {
        private readonly ITrackInfoService _trackInfoService;

        public TrackInfoController(ITrackInfoService trackInfoService)//, IHubContext<MessageHub> hubContext
        {
            _trackInfoService = trackInfoService;
        }

        [HttpGet("list")]
        public IActionResult QueryTrackInfo([FromQuery] TrackInfoQueryDto parm)
        {
            var response = _trackInfoService.GetTrackInfoByAlarmId(parm);
            return Message(response);
        }
    }
}
