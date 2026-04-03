using Infrastructure.Model;
using Microsoft.AspNetCore.Mvc;

namespace Lan.Application.Controllers
{
    [Route("api/system/user")]
    [ApiController]
    public class SysUserController : BaseController
    {
        private readonly ISysUserService UserService;
        public SysUserController(ISysUserService userService)
        {
            UserService = userService;
        }

        [HttpGet("list")]
        public IActionResult List([FromQuery] SysUserQueryDto user)
        {
            var list = UserService.SelectUserList(user);

            return Message(list);
        }

        [HttpGet("{userId:int=0}")]
        public IActionResult GetInfo(int userId)
        {
            SysUser sysUser = UserService.GetSysUser(userId);
            return Message(sysUser);
        }

        /// <summary>
        /// 添加用户
        /// </summary>
        /// <param name="parm"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult AddUser([FromBody] SysUserDto parm)
        {
            var user = parm.Adapt<SysUser>().ToCreate(HttpContext);
            user.Password = "admin";

            return Message(UserService.AddSysUser(user));
        }

        /// <summary>
        /// 修改用户
        /// </summary>
        /// <param name="parm"></param>
        /// <returns></returns>
        [HttpPut]
        public IActionResult UpdateUser([FromBody] SysUserDto parm)
        {
            var user = parm.Adapt<SysUser>().ToUpdate(HttpContext);

            int upResult = UserService.UpdateSysUser(user);

            return ToResponse(upResult);
        }

        [HttpDelete("{userId}")]
        public IActionResult Remove(int userid = 0)
        {

            int result = UserService.DeleteSysUser(userid);

            return ToResponse(result);
        }
    }
}
