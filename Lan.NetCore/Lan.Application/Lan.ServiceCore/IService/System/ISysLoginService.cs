using Lan.Dto;
using Lan.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.ServiceCore.IService
{
    public interface ISysLoginService
    {
        public SysUser Login(LoginBodyDto loginBody);
    }
}
