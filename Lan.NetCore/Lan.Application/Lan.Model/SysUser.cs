using Model;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lan.Model
{
    /// <summary>
    /// 用户表
    /// </summary>
    [SugarTable("sys_user", "用户表")]
    [Tenant("0")]
    public class SysUser : SysBase
    {
        /// <summary>
        /// 用户id
        /// </summary>
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
        public int UserId { get; set; }
        /// <summary>
        /// 登录用户名
        /// </summary>
        [SugarColumn(Length = 30, ColumnDescription = "用户账号")]
        public string UserName { get; set; }
        /// <summary>
        /// 用户昵称
        /// </summary>
        [SugarColumn(Length = 30, ColumnDescription = "用户昵称")]
        public string NickName { get; set; }
        /// <summary>
        /// 用户类型（00系统用户）
        /// </summary>
        [SugarColumn(Length = 2, ColumnDescription = "用户类型（00系统用户）", DefaultValue = "00")]
        public string UserType { get; set; } = "00";
        //[SugarColumn(IsOnlyIgnoreInsert = true)]
        public string Avatar { get; set; }
        [SugarColumn(Length = 50, ColumnDescription = "用户邮箱")]
        public string Email { get; set; }

        [JsonIgnore]
        [SugarColumn(Length = 100, ColumnDescription = "密码")]
        public string Password { get; set; }
        /// <summary>
        /// 手机号
        /// </summary>
        public string Phonenumber { get; set; }
        /// <summary>
        /// 用户性别（0男 1女 2未知）
        /// </summary>
        public int Sex { get; set; }

        /// <summary>
        /// 帐号状态（0正常 1停用）
        /// </summary>
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }

        /// <summary>
        /// 删除标志（0代表存在 2代表删除）
        /// </summary>
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }

        /// <summary>
        /// 最后登录IP
        /// </summary>
        [SugarColumn(IsOnlyIgnoreInsert = true)]
        public string LoginIP { get; set; }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        [SugarColumn(IsOnlyIgnoreInsert = true)]
        public DateTime? LoginDate { get; set; }

        /// <summary>
        /// 部门Id
        /// </summary>
        [SugarColumn(DefaultValue = "0")]
        public long DeptId { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
    }
}
