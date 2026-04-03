using Lan.ServiceCore.Signalr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Lan.Application.Controllers.Base
{
    [Route("api/commoninterface")]
    [ApiController]
    public class CommonInterfaceController : BaseController
    {
        private readonly IHubContext<MessageHub> _hubContext;
        private readonly ICameraService _CameraService;

        public CommonInterfaceController(IHubContext<MessageHub> hubContext, ICameraService cameraService)
        {
            _hubContext = hubContext;
            _CameraService = cameraService;
        }

        [HttpGet("open/{Ip}")]
        public IActionResult OpenCamera(string Ip)
        {
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                // DateFormatString = timeFormatStr
            };

            CameraModel cameraModel = _CameraService.GetInfo(Ip);
            if (cameraModel != null)
            {
                CameraInfo cameraInfo = new()
                {
                    CameraIp = cameraModel.Ip,
                    UserName = cameraModel.Username,
                    PassWord = cameraModel.Password,
                    CameraURL = cameraModel.CameraURL,
                };

                string json = JsonConvert.SerializeObject(cameraInfo, Formatting.Indented, serializerSettings);

                _hubContext.Clients.All.SendAsync("PlayStream", json);
                Thread.Sleep(1);
                _hubContext.Clients.All.SendAsync("ShowWindow", "show");
            }
            return Message("OK");
        }

        [HttpGet("close")]
        public IActionResult CloseCamera()
        {
            _hubContext.Clients.All.SendAsync("CloseStream", "CloseStream");
            Thread.Sleep(1);
            _hubContext.Clients.All.SendAsync("HideWindow", "Hide");

            return Message("OK");
        }

    }
}
