using Lan.Dto;
using Lan.Infrastructure.CameraOnvif;
using MemoryCache.Core;
using Microsoft.AspNetCore.Mvc;
using Model;
using System.Text;

namespace Lan.Application.Controllers
{
    [Route("api/camera")]
    [ApiController]
    public class CameraController : BaseController
    {
        public readonly ICameraService _CameraService;
        private readonly IMemoryCacheService _cache;
        public CameraController(ICameraService cameraService, IMemoryCacheService cache)
        {
            _CameraService = cameraService;
            _cache = cache;
        }

        [HttpGet("list")]
        public IActionResult QueryRadar([FromQuery] CameraQueryDto parm)
        {
            var response = _CameraService.GetList(parm);
            return Message(response);
        }

        [HttpGet("preview")]
        public async Task<IActionResult> GetCameraPreview()
        {
            var response = await _CameraService.GetListPreviewAsync();
            var info = response.Adapt<List<CameraDto>>();
            return Message(info);
        }

        [HttpGet("{Id}")]
        public IActionResult GetCamera(int Id)
        {
            var response = _CameraService.GetInfo(Id);
            var info = response.Adapt<CameraDto>();
            return Message(info);
        }

        [HttpPost]
        public IActionResult AddCamera([FromBody] CameraDto parm)
        {
            ONVIF_MANAGEMENT_CAPABILITIES capabilities = new ONVIF_MANAGEMENT_CAPABILITIES();
            int ret = onvifsdk.ONVIF_MAGEMENT_GetCapabilitiesEx(20, parm.Ip, 80, parm.Username, parm.Password, ref capabilities);
            string strCameraURL = "";
            string strSourceToken = "";

            if (ret == 0)
            {
                ONVIF_COMMON_INFO common = new ONVIF_COMMON_INFO()
                {
                    username = parm.Username,
                    password = parm.Password,
                    onvifUrls = capabilities.onvifUrls
                };

                ONVIF_MEDIA_CHANNEL_SOURCE channel = new ONVIF_MEDIA_CHANNEL_SOURCE();
                ret = onvifsdk.ONVIF_MEDIA_GetChannelSource(3, ref common, ref channel);

                if (ret == 0 && channel.sourceNum > 0)
                {
                    strSourceToken = Encoding.ASCII.GetString(channel.sourceToken[0].sourceToken).TrimEnd('\0');

                    common.sourceToken = strSourceToken;
                    byte[] rtspUrl = new byte[1024];

                    ret = onvifsdk.ONVIF_MEDIA_GetStreamUri(3, ref common, rtspUrl);
                    if (ret == 0)
                    {
                        strCameraURL = Encoding.ASCII.GetString(rtspUrl).TrimEnd('\0');
                    }
                }

                //MemoryCacheHelper.Set(parm.Ip, common);
                _cache.Set(parm.Ip, common);
            }

            parm.CameraURL = strCameraURL;
            parm.CameraId = 0;
            parm.SourceToken = strSourceToken;

            if (string.IsNullOrEmpty(parm.CameraURL))
            {
                return ToResponse(ResultCode.PARAM_ERROR, "Failed to obtain the stream address, save failed!");
            }

            var modal = parm.Adapt<CameraModel>().ToCreate(HttpContext);
            var bl = _CameraService.GetInfoByIp(modal.Ip);
            if (bl)
            {
                return ToResponse(ResultCode.DATA_REPEAT, "The IP address already exists. Save failed!");
            }
            var response = _CameraService.AddCamera(modal);
            return Message(response);
        }

        [HttpPut]
        public IActionResult UpdateCamera([FromBody] CameraDto parm)
        {
            var modal = parm.Adapt<CameraModel>().ToUpdate(HttpContext);
            var response = _CameraService.UpdateCamera(modal);

            return ToResponse(response);
        }

        [HttpDelete("delete/{ids}")]
        public IActionResult DeleteCamera([FromRoute] string ids)
        {
            var idArr = Lan.Tools.Tools.SplitAndConvert<int>(ids);

            return ToResponse(_CameraService.DeleteCamera(idArr));
        }

        [HttpPost("rjadd")]
        public IActionResult GetRepetitionJudgmentAdd([FromBody] CameraQueryDto1 parm)
        {
            var modal = parm.Adapt<DefenceareaModel>().ToUpdate(HttpContext);

            if (parm.CameraIds?.Length > 0)
            {
                string str = _CameraService.RepetitionJudgment(parm.CameraIds);
                return ToResponse(ResultCode.RepetitionJudgment, str.Trim());
            }

            return Message("OK");
        }
        [HttpPost("rjedit")]
        public IActionResult GetRepetitionJudgmentEdit([FromBody] CameraQueryDto2 parm)
        {
            var modal = parm.Adapt<DefenceareaModel>().ToUpdate(HttpContext);

            if (parm.CameraIds?.Length > 0)
            {
                string str = _CameraService.RepetitionJudgmentEdit(parm.BindingAreaId, parm.CameraIds);
                return ToResponse(ResultCode.RepetitionJudgment, str.Trim());
            }

            return Message("OK");
        }

        [HttpGet("dcamera/{DefenceAreaId}")]
        public IActionResult GetCameraByDefenceAreaId(int DefenceAreaId)
        {
            var response = _CameraService.GetListCameraByDefenceAreaId(DefenceAreaId);
            return Message(response);
        }

        [HttpGet("min/{Id}/{Ip}")]
        public IActionResult GetMinZoomPT(int Id, string Ip)
        {
            _CameraService.GetMinZoomPT(Id, Ip);
            return Message("OK");
        }
        [HttpGet("max/{Id}/{Ip}")]
        public IActionResult GetMaxZoomPT(int Id, string Ip)
        {
            _CameraService.GetMaxZoomPT(Id, Ip);
            return Message("OK");
        }
    }
}
