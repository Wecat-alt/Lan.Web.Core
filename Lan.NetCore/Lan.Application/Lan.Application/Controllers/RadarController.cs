using Lan.Dto;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.Services;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace Lan.Application.Controllers
{
    [Route("api/radar")]
    [ApiController]
    public class RadarController(IRadarService rs) : BaseController
    {

        [HttpGet("list")]
        public IActionResult QueryRadar([FromQuery] RadarQueryDto parm) => Message(rs.GetList(parm));

        [HttpGet("all")]
        public IActionResult QueryRadarAll() => Message(rs.GetListALL());

        [HttpGet("{Id}")]
        public IActionResult GetRadar(int Id) => Message(rs.GetInfo(Id).Adapt<RadarDto>());

        [HttpPost]
        public IActionResult AddRadar([FromBody] RadarDto parm)
        {
            var modal = parm.Adapt<RadarModel>().ToCreate(HttpContext);
            return rs.GetInfoByIp(parm.Ip)
                ? ToResponse(ResultCode.DATA_REPEAT, "The IP address already exists. Save failed!")
                : Message(rs.AddRadar(modal));
        }

        [HttpPut]
        public IActionResult UpdateRadar([FromBody] RadarDto parm)
            => ToResponse(rs.UpdateRadar(parm.Adapt<RadarModel>().ToUpdate(HttpContext)));

        [HttpGet("setLatLng/{Ip}/{Lat}/{Lng}")]
        public IActionResult SetCalibrationTrack(string Ip, string Lat, string Lng)
        {
            rs.UpdateRadarLatLng(Ip, Lat, Lng);
            return Message("OK");
        }

        [HttpDelete("delete/{ids}")]
        public IActionResult DeleteRadar([FromRoute] string ids)
            => ToResponse(rs.DeleteRadar(Lan.Tools.Tools.SplitAndConvert<int>(ids)));

        [HttpGet("listby/{AreaId}")]
        public IActionResult GetRadarByAreaId(int AreaId) => Message(rs.GetListByAreaId(AreaId));

        [HttpPost("rjadd")]
        public IActionResult GetRepetitionJudgmentAdd([FromBody] RadarQueryDto1 parm)
            => parm.RadarIds is { Length: > 0 }
                ? ToResponse(ResultCode.RepetitionJudgment, rs.RepetitionJudgment(parm.RadarIds).Trim())
                : Message("OK");

        [HttpPost("rjedit")]
        public IActionResult GetRepetitionJudgmentEdit([FromBody] RadarQueryDto2 parm)
            => parm.RadarIds is { Length: > 0 }
                ? ToResponse(ResultCode.RepetitionJudgment, rs.RepetitionJudgmentEdit(parm.BindingAreaId, parm.RadarIds).Trim())
                : Message("OK");
    }
}
