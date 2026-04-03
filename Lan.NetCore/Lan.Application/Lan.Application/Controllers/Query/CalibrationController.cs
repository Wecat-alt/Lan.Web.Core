using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lan.Application.Controllers.Query
{
    [Route("api/calibration")]
    [ApiController]
    public class CalibrationController : BaseController
    {
        private readonly ICalibrationService _CalibrationService;
        private readonly ICameraService _CameraService;
        private readonly IRadarService _RadarService;
        private readonly IDefenceareaService _DefenceareaService;

        public CalibrationController(ICalibrationService CalibrationService, IDefenceareaService defenceareaService, ICameraService cameraService, IRadarService radarService)
        {
            _CalibrationService = CalibrationService;
            _DefenceareaService = defenceareaService;
            _CameraService = cameraService;
            _RadarService = radarService;

        }

        [HttpPost]
        public IActionResult AddDefencearea([FromBody] CalibrationDto parm)
        {
            var modal = parm.Adapt<Calibration>().ToCreate(HttpContext);

            var response = _CalibrationService.AddCalibration(modal);

            return Message(response);
        }

        [HttpGet("{Type}/{ips}/{speed}")]
        public IActionResult GetCamera(string Type, string ips, string speed)
        {
            _CalibrationService.cameraPTZ(Type, ips, speed);
            return Message("OK");
        }
        [HttpGet("stop/{ips}")]
        public IActionResult PtzStop(string ips)
        {
            _CalibrationService.PtzStop(ips);
            return Message("OK");
        }

        /// <summary>
        /// 开始跟踪/结束跟踪
        /// </summary>
        /// <param name="Status"></param>
        /// <returns></returns>
        [HttpGet("set/{Id}/{IsTrack}")]
        public IActionResult SetCalibrationTrack(int Id, int IsTrack)
        {
            _CameraService.UpdateIsTrack(Id, IsTrack);

            //_CalibrationService.SetTrack(Status);
            return Message("OK");
        }

        [HttpGet("msg/{CameraIp}/{ZoneId}")]
        public IActionResult GetCalibration(string CameraIp, int ZoneId)
        {
            var response = _CalibrationService.GetInfo(CameraIp, ZoneId);

            var info = response.Adapt<CalibrationDto>();
            return Message(info);
        }

        [HttpGet("up/{a}/{b}/{c}/{d}/{e}/{f}")]
        public IActionResult UpdateCalibration(string a, int b, float c, float d, float e, float f)
        {
            _CalibrationService.UpdateCalibration(a, b, c, d, e, f);
            return Message("OK");
        }
    }
}
