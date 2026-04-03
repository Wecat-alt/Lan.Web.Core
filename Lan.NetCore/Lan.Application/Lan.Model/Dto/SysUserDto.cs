using Lan.Model;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Dto
{
    public class SysUserDto
    {
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string NickName { get; set; }
        public string Password { get; set; }
        public string Phonenumber { get; set; }
    }

    public class SysUserQueryDto : PagerInfo
    {
        public string? UserName { get; set; }
        public string? Phonenumber { get; set; }
    }
}
