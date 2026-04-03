using Infrastructure;
using Lan.Dto;
using Lan.Model;
using Lan.Repository;
using Lan.ServiceCore.IService;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.ServiceCore.Services.System
{
    [AppService(ServiceType = typeof(ISysLoginService), ServiceLifetime = LifeTime.Transient)]
    public class SysLoginService : Repository<SysLogininfor>, ISysLoginService
    {
        private readonly ISysUserService SysUserService;

        public SysLoginService(ISysUserService sysUserService)
        {
            SysUserService = sysUserService;
        }

        public SysUser Login(LoginBodyDto loginBody)
        {
            
            SysUser user = SysUserService.Login(loginBody);

            //if (user == null || user.UserId <= 0)
            //{
            //    logininfor.Msg = "用户名或密码错误";
            //    AddLoginInfo(logininfor);
            //    throw new CustomException(ResultCode.LOGIN_ERROR, logininfor.Msg, false);
            //}
            //if (user.Status == 1)
            //{
            //    logininfor.Msg = "该用户已禁用";
            //    AddLoginInfo(logininfor);
            //    throw new CustomException(ResultCode.LOGIN_ERROR, logininfor.Msg, false);
            //}
            return user;
        }
    }
}
