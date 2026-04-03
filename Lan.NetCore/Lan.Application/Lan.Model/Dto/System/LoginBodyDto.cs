using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Dto
{
    public class LoginBodyDto
    {
        [Required(ErrorMessage = "用户名不能为空")]
        public string Username { get; set; }
        [Required(ErrorMessage = "密码不能为空")]
        public string Password { get; set; }
        public string? Code { get; set; }
        public string? Uuid { get; set; } = "";
        public string? LoginIP { get; set; }
        public string? ClientId { get; set; }
    }
}
