using Lan.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Azure.Core.HttpHeader;

namespace Lan.Application.Controllers.System
{
    [Route("api/system/dict/data")]
    [ApiController]
    public class SysDictDataController : BaseController
    {
        private readonly ISysDictDataService SysDictDataService;
        private readonly ISysDictService SysDictService;

        public SysDictDataController(ISysDictService sysDictService, ISysDictDataService sysDictDataService)
        {
            SysDictService = sysDictService;
            SysDictDataService = sysDictDataService;
        }

        /// <summary>
        /// 搜索
        /// </summary>
        /// <param name="dictData"></param>
        /// <param name="pagerInfo"></param>
        /// <returns></returns>
        [HttpGet("list")]
        public IActionResult List([FromQuery] SysDictData dictData, [FromQuery] PagerInfo pagerInfo)
        {
            var list = SysDictDataService.SelectDictDataList(dictData, pagerInfo);

            //if (dictData.DictType.StartsWith("sql_"))
            //{
            //    var result = SysDictService.SelectDictDataByCustomSql(dictData.DictType);

            //    list.Result.AddRange(result);
            //    list.TotalNum += result.Count;
            //}
            return Message(list);
        }

        /// <summary>
        /// 根据字典类型查询字典数据信息
        /// </summary>
        /// <param name="dictType"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("type/{dictType}")]
        public IActionResult DictType(string dictType)
        {
            return Message(SysDictDataService.SelectDictDataByType(dictType));
        }

        /// <summary>
        /// 根据字典类型查询字典数据信息
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("types")]
        public IActionResult DictTypes([FromBody] List<SysdictDataDto> dto)
        {
            var list = SysDictDataService.SelectDictDataByTypes(dto.Select(f => f.DictType).ToArray());
            List<SysdictDataDto> dataVos = new();

            foreach (var dic in dto)
            {
                SysdictDataDto vo = new()
                {
                    DictType = dic.DictType,
                    ColumnName = dic.ColumnName,
                    List = list.FindAll(f => f.DictType == dic.DictType)
                };
                if (dic.DictType.StartsWith("cus_") || dic.DictType.StartsWith("sql_"))
                {
                    vo.List.AddRange(SysDictService.SelectDictDataByCustomSql(dic.DictType));
                }
                dataVos.Add(vo);
            }
            return Message(dataVos);
        }

        /// <summary>
        /// 查询字典数据详细
        /// </summary>
        /// <param name="dictCode"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("info/{dictCode}")]
        public IActionResult GetInfo(long dictCode)
        {
            return Message(SysDictDataService.SelectDictDataById(dictCode));
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        [HttpPost()]
        public IActionResult Add([FromBody] SysDictData dict)
        {
            //dict.Create_by = HttpContext.GetName();
            //dict.Create_time = DateTime.Now;
            return Message(SysDictDataService.InsertDictData(dict));
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        [HttpPut()]
        public IActionResult Edit([FromBody] SysDictData dict)
        {
            //dict.Update_by = HttpContext.GetName();
            return Message(SysDictDataService.UpdateDictData(dict));
        }

        /// <summary>
        /// 删除字典类型
        /// </summary>
        /// <param name="dictCode"></param>
        /// <returns></returns>
        [HttpDelete("{dictCode}")]
        public IActionResult Remove(string dictCode)
        {
            long[] dictCodes = Lan.Tools.Tools.SpitLongArrary(dictCode);

            return Message(SysDictDataService.DeleteDictDataByIds(dictCodes));
        }
    }
}
