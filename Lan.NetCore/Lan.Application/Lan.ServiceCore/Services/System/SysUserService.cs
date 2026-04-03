using Dm.util;
using Infrastructure;
using Lan.Dto;
using Lan.Model;
using Lan.Repository;
using Lan.ServiceCore.IService;
using Model;
using SqlSugar;

namespace Lan.ServiceCore.Services
{
    [AppService(ServiceType = typeof(ISysUserService), ServiceLifetime = LifeTime.Transient)]
    public class SysUserService : Repository<SysUser>, ISysUserService
    {
        public PagedInfo<SysUser> SelectUserList(SysUserQueryDto user)
        {
            var exp = Expressionable.Create<SysUser>();
            exp.AndIF(!string.IsNullOrEmpty(user.UserName), u => u.UserName.Contains(user.UserName));
            exp.AndIF(!string.IsNullOrEmpty(user.Phonenumber), u => u.Phonenumber.Contains(user.Phonenumber));

            var query = Queryable()
            .Where(exp.ToExpression())
            .OrderBy(u => u.UserId, OrderByType.Asc);

            return query.ToPage(user);
        }

        public SysUser GetSysUser(int Id)
        {
            var response = Queryable()
                .Where(x => x.UserId == Id)
                .First();
            return response;
        }

        public SysUser AddSysUser(SysUser model)
        {
            return Insertable(model).ExecuteReturnEntity();
        }

        public int UpdateSysUser(SysUser model)
        {
            return Update(model, true);
        }
        public int DeleteSysUser(object id)
        {
            return Delete(id);
        }

        public SysUser Login(LoginBodyDto user)
        {
            string pwd = user.Password.Replace(' ', 'h');
            var response = Queryable()
               .Where(x => x.UserName == user.Username && x.Password == pwd)
               .First();

            return response;
        }

    }
}
