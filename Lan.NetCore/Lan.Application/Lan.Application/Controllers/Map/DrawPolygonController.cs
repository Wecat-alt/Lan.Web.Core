using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Web;

namespace Lan.Application.Controllers.Map
{
    [Route("api/drawpolygon")]
    [ApiController]
    public class DrawPolygonController : BaseController
    {
        private readonly IDrawPolygonService _drawPolygonService;

        public DrawPolygonController(IDrawPolygonService drawPolygonService)
        {
            _drawPolygonService = drawPolygonService;
        }

        [HttpGet("list")]
        public IActionResult QueryRadar()
        {
            var response = _drawPolygonService.GetList();
            return Message(response);
        }

        [HttpGet("{Id}")]
        public IActionResult GetRadar(int Id)
        {
            var response = _drawPolygonService.GetInfo(Id);

            var info = response.Adapt<DrawPolygonDto>();
            return Message(info);
        }

        [HttpPost]
        public IActionResult AddDrawPolygon([FromBody] DrawPolygonDto parm)
        {
            var modal = parm.Adapt<DrawPolygon>().ToCreate(HttpContext);

            var response = _drawPolygonService.AddDrawPolygon(modal);

            return Message(response);
        }

        [HttpPut]
        public IActionResult UpdateDrawPolygon([FromBody] DrawPolygonDto parm)
        {
            var modal = parm.Adapt<DrawPolygon>().ToUpdate(HttpContext);
            var response = _drawPolygonService.UpdateDrawPolygon(modal);

            return ToResponse(response);
        }

        [HttpDelete("delete")]
        public IActionResult DeleteDrawPolygon([FromBody] string ids)
        {
            return ToResponse(_drawPolygonService.DeleteDrawPolygon(HttpUtility.UrlDecode(ids))); ;
        }
    }
}
