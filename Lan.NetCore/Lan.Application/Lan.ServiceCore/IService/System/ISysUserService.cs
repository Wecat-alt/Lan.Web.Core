using Lan.Dto;
using Lan.Model;

namespace Lan.ServiceCore.IService
{
    public interface ISysUserService
    {
        public PagedInfo<SysUser> SelectUserList(SysUserQueryDto user);
        SysUser Login(LoginBodyDto user);

        SysUser GetSysUser(int Id);
        SysUser AddSysUser(SysUser parm);
        int UpdateSysUser(SysUser parm);
        int DeleteSysUser(object id);
    }
}
