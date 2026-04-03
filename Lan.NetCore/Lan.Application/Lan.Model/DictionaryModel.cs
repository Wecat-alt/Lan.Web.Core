using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    [SugarTable("dictionary")]
    public class DictionaryModel
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public int D_Type { get; set; }
        [SugarColumn(Length = 100)]
        public string D_Key { get; set; }
        [SugarColumn(Length = 100)]
        public string D_Value { get; set; }
    }
}
