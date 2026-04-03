using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Data;
using System.Reflection.Metadata;

namespace Lan.Application.Controllers.System
{
    [Route("api/login")]
    [ApiController]
    public class SysLoginController : BaseController
    {
        private readonly ISysUserService sysUserService;
        private readonly ISysLoginService sysLoginService;
        private readonly ISysConfigService sysConfigService;


        public SysLoginController(
    ISysUserService sysUserService,
    ISysLoginService sysLoginService,
    ISysConfigService configService)
        {
            this.sysUserService = sysUserService;
            this.sysLoginService = sysLoginService;
            this.sysConfigService = configService;
        }
        [HttpPost]
        public IActionResult Login([FromBody] LoginBodyDto loginBody)
        {
            if (loginBody == null)
            {
                Console.Write("请求参数错误");
            }
            var user = sysLoginService.Login(loginBody);
            if (user == null || user.UserId <= 0)
            {
                return ToResponse(ResultCode.LOGIN_ERROR, "用户登录失败，请检查用户名和密码！");
            }

            return Message(user);
        }
    }
}
