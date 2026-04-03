using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lan.Application.Controllers.Base
{
    [Route("api/trackparameter")]
    [ApiController]
    public class TrackParameterController : BaseController
    {
        private readonly ITrackParameterService _trackParameterService;

        public TrackParameterController(ITrackParameterService trackParameterService)//, IHubContext<MessageHub> hubContext
        {
            _trackParameterService = trackParameterService;
        }

        [HttpGet("{Id}")]
        public IActionResult GetTrackParameter(int Id)
        {
            var response = _trackParameterService.GetInfo(Id);
            var info = response.Adapt<TrackParameter>();
            return Message(info);
        }
    }
}
