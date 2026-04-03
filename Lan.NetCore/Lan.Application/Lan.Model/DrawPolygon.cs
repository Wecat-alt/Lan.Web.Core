using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    [SugarTable("drawpolygon")]
    public class DrawPolygon
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int DrawId { get; set; }
        public int DefenceAreaId { get; set; }
        [SugarColumn(Length = 1000)]
        public string pointListLatLng { get; set; }
        public int Status { get; set; }
        public DateTime DateTime { get; set; } = DateTime.Now;
    }
}
