using Azure;
using Lan.Dto;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Lan.Application.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DefenceAreaController : BaseController
    {
        /// <summary>
        /// 接口
        /// </summary>
        private readonly IDefenceareaService _DefenceareaService;
        private readonly ICameraService _CameraService;
        private readonly IRadarService _RadarService;

        public DefenceAreaController(IDefenceareaService DefenceareaService, ICameraService cameraService, IRadarService radarService)
        {
            _DefenceareaService = DefenceareaService;
            _CameraService = cameraService;
            _RadarService = radarService;
        }

        [HttpGet("list")]
        public IActionResult QueryDefencearea([FromQuery] DefenceareaQueryDto parm)
        {
            var response = _DefenceareaService.GetList(parm);
            return Message(response);
        }

        [HttpGet("{Id}")]
        public IActionResult GetDefencearea(int Id)
        {
            Dictionary<string, object> dic = new();

            var cameras = _CameraService.GetAllList();
            var radar = _RadarService.GetAllList();
            dic.Add("cameras", cameras);
            dic.Add("radars", radar);

            if (Id > 0)
            {
                DefenceareaModel defencearea = _DefenceareaService.GetInfo(Id);
                dic.Add("defencearea", defencearea);
                dic.Add("cameraIds", defencearea.CameraIds);
                dic.Add("radarIds", defencearea.RadarIds);
            }

            return Message(dic);
        }

        [HttpPost]
        public IActionResult AddDefencearea([FromBody] DefenceareaDto parm)
        {
            var modal = parm.Adapt<DefenceareaModel>().ToCreate(HttpContext);
            var bl = _DefenceareaService.GetInfoByName(parm.Name);
            if (bl)
            {
                return ToResponse(ResultCode.DATA_REPEAT, "The zone name is duplicated. Save failed!");
            }
            var response = _DefenceareaService.AddDefencearea(modal);

            return Message(response);
        }

        [HttpPut]
        public IActionResult UpdateDefencearea([FromBody] DefenceareaDto parm)
        {
            try
            {
                var modal = parm.Adapt<DefenceareaModel>().ToUpdate(HttpContext);
                var bl = _DefenceareaService.GetInfoByName(parm.Name, parm.Id);
                if (bl)
                {
                    return ToResponse(ResultCode.DATA_REPEAT, "The zone name is duplicated. Save failed!");
                }
                var response = _DefenceareaService.UpdateDefencearea(modal);

                return ToResponse(response);
            }
            catch (Exception ex )
            {

                throw;
            }
            
        }
        [HttpPut("enable/{status}")]
        public IActionResult UpdateEnableDefencearea([FromRoute] int status)
        {
            var res = _DefenceareaService.EnableDefencearea(status);
            return Message(res);
        }


        [HttpDelete("delete/{ids}")]
        public IActionResult DeleteDefencearea([FromRoute] string ids)
        {
            var idArr = Lan.Tools.Tools.SplitAndConvert<int>(ids);

            return ToResponse(_DefenceareaService.DeleteDefencearea(idArr));
        }

        [HttpGet("all")]
        public IActionResult GetAllDeleteDefence()
        {
            var response = _DefenceareaService.GetAllList();
            return Message(response);
        }

        [HttpGet("treeselect")]
        public IActionResult TreeSelect([FromQuery] DefenceareaQueryDto dept)
        {
            var depts = _DefenceareaService.GetList(dept);

            List<DefenceareaDtoParent> l_defenceareaDtoParents = new List<DefenceareaDtoParent>();
            DefenceareaDtoParent defenceareaDtoParent = new DefenceareaDtoParent();
            defenceareaDtoParent.Id = 0;
            defenceareaDtoParent.Name = "All";
            defenceareaDtoParent.ParentId = -1;
            l_defenceareaDtoParents.Add(defenceareaDtoParent);


            foreach (var item in depts)
            {
                DefenceareaDtoParent defenceareaDtoParent1 = new DefenceareaDtoParent();
                defenceareaDtoParent1.Id = item.Id;
                defenceareaDtoParent1.Name = item.Name;
                defenceareaDtoParent1.ParentId = 0;
                l_defenceareaDtoParents.Add(defenceareaDtoParent1);
            }

            return Message(_DefenceareaService.BuildDeptTreeSelect(l_defenceareaDtoParents), TIME_FORMAT_FULL);
        }

    }
}
