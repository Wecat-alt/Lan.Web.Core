using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Infrastructure
{
    public class OptionsSettings
    {
        public List<DbConfigs> DbConfigs { get; set; }
    }
    public class DbConfigs
    {
        public string Conn { get; set; }
        public int DbType { get; set; }
        public string ConfigId { get; set; }
        public bool IsAutoCloseConnection { get; set; }
        public string DbName { get; set; }
    }
}
