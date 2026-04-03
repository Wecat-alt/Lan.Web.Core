using Infrastructure.Model;
using Lan.Model;
using Microsoft.AspNetCore.Mvc;

namespace Lan.Application.Controllers.System
{
    [Route("api/ystem/dict/type")]
    [ApiController]
    public class SysDictTypeController : BaseController
    {
        private readonly ISysDictService SysDictService;

        public SysDictTypeController(ISysDictService sysDictService)
        {
            SysDictService = sysDictService;
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="pagerInfo"></param>
        /// <returns></returns>
        [HttpGet("list")]
        public IActionResult List([FromQuery] SysDictType dict, [FromQuery] PagerInfo pagerInfo)
        {
            var list = SysDictService.SelectDictTypeList(dict, pagerInfo);

            return Message(list, TIME_FORMAT_FULL);
        }

        /// <summary>
        /// 查询字典类型详细
        /// </summary>
        /// <param name="dictId"></param>
        /// <returns></returns>
        [HttpGet("{dictId}")]
        public IActionResult GetInfo(long dictId = 0)
        {
            return Message(SysDictService.GetInfo(dictId));
        }

        /// <summary>
        /// 添加字典类型
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("edit")]
        public IActionResult Add([FromBody] SysDictTypeDto dto)
        {
            SysDictType dict = dto.Adapt<SysDictType>();
            //if (UserConstants.NOT_UNIQUE.Equals(SysDictService.CheckDictTypeUnique(dict)))
            //{
            //    return ToResponse(ApiResult.Error($"新增字典'{dict.DictName}'失败，字典类型已存在"));
            //}
            //dict.Create_by = HttpContext.GetName();
            //dict.Create_time = DateTime.Now;
            return Message(SysDictService.InsertDictType(dict));
        }

        /// <summary>
        /// 修改字典类型
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [Route("edit")]
        [HttpPut]
        public IActionResult Edit([FromBody] SysDictTypeDto dto)
        {
            SysDictType dict = dto.Adapt<SysDictType>();
            //if (UserConstants.NOT_UNIQUE.Equals(SysDictService.CheckDictTypeUnique(dict)))
            //{
            //    return ToResponse(ApiResult.Error($"修改字典'{dict.DictName}'失败，字典类型已存在"));
            //}
            //设置添加人
            //dict.Update_by = HttpContext.GetName();
            return Message(SysDictService.UpdateDictType(dict));
        }

        /// <summary>
        /// 删除字典类型
        /// </summary>
        /// <returns></returns>
        [HttpDelete("{ids}")]
        public IActionResult Remove(string ids)
        {
            long[] idss = Lan.Tools.Tools.SpitLongArrary(ids);

            return Message(SysDictService.DeleteDictTypeByIds(idss));
        }

        /// <summary>
        /// 字典导出
        /// </summary>
        /// <returns></returns>
        [HttpGet("export")]
        public IActionResult Export()
        {
            var list = SysDictService.GetAll();

            string sFileName = "";// ExportExcel(list, "sysdictType", "字典");
            return Message(new { path = "/export/" + sFileName, fileName = sFileName });
        }
    }
}
