using Lan.Dto;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.Services;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace Lan.Application.Controllers
{
    [Route("api/radar")]
    [ApiController]
    public class RadarController : BaseController
    {
        private readonly IRadarService _RadarService;

        public RadarController(IRadarService RadarService)
        {
            _RadarService = RadarService;
        }

        [HttpGet("list")]
        public IActionResult QueryRadar([FromQuery] RadarQueryDto parm)
        {
            var response = _RadarService.GetList(parm);
            return Message(response);
        }

        [HttpGet("all")]
        public IActionResult QueryRadarAll()
        {
            var response = _RadarService.GetListALL();
            return Message(response);
        }

        [HttpGet("{Id}")]
        public IActionResult GetRadar(int Id)
        {
            var response = _RadarService.GetInfo(Id);

            var info = response.Adapt<RadarDto>();
            return Message(info);
        }

        [HttpPost]
        public IActionResult AddRadar([FromBody] RadarDto parm)
        {
            var modal = parm.Adapt<RadarModel>().ToCreate(HttpContext);

            var bl = _RadarService.GetInfoByIp(parm.Ip);
            if (bl)
            {
                return ToResponse(ResultCode.DATA_REPEAT, "The IP address already exists. Save failed!");
            }

            var response = _RadarService.AddRadar(modal);

            return Message(response);
        }

        [HttpPut]
        public IActionResult UpdateRadar([FromBody] RadarDto parm)
        {
            var modal = parm.Adapt<RadarModel>().ToUpdate(HttpContext);
            
            var response = _RadarService.UpdateRadar(modal);

            return ToResponse(response);
        }
        [HttpGet("setLatLng/{Ip}/{Lat}/{Lng}")]
        public IActionResult SetCalibrationTrack(string Ip, string Lat, string Lng)
        {
            _RadarService.UpdateRadarLatLng( Ip, Lat, Lng);
            return Message("OK");
        }

        [HttpDelete("delete/{ids}")]
        public IActionResult DeleteRadar([FromRoute] string ids)
        {
            int[] idArr = Lan.Tools.Tools.SplitAndConvert<int>(ids);

            return ToResponse(_RadarService.DeleteRadar(idArr));
        }
        [HttpGet("listby/{AreaId}")]
        public IActionResult GetRadarByAreaId(int AreaId)
        {
            var response = _RadarService.GetListByAreaId(AreaId);
            return Message(response);
        }
        [HttpPost("rjadd")]
        public IActionResult GetRepetitionJudgmentAdd([FromBody] RadarQueryDto1 parm)
        {
            var modal = parm.Adapt<DefenceareaModel>().ToUpdate(HttpContext);

            if (parm.RadarIds?.Length > 0)
            {
                string str = _RadarService.RepetitionJudgment(parm.RadarIds);
                return ToResponse(ResultCode.RepetitionJudgment, str.Trim());
            }

            return Message("OK");
        }
        [HttpPost("rjedit")]
        public IActionResult GetRepetitionJudgmentEdit([FromBody] RadarQueryDto2 parm)
        {
            var modal = parm.Adapt<DefenceareaModel>().ToUpdate(HttpContext);

            if (parm.RadarIds?.Length > 0)
            {
                string str = _RadarService.RepetitionJudgmentEdit(parm.BindingAreaId, parm.RadarIds);
                return ToResponse(ResultCode.RepetitionJudgment, str.Trim());
            }

            return Message("OK");
        }
    }
}
