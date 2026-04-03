using Lan.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Dto
{
    public class SysConfigDto
    {
        public int ConfigId { get; set; }
        public string ConfigName { get; set; }
        public string ConfigKey { get; set; }
        public string ConfigValue { get; set; }
        public string ConfigType { get; set; }
    }
    public class SysConfigQueryDto : PagerInfo
    {
        public string? ConfigName { get; set; }
        public string? ConfigKey { get; set; }
    }
}
