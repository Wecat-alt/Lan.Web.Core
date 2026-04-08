using Lan.Model;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Dto
{
    public class DrawPolygonQueryDto : PagerInfo
    {
        public int? DrawID { get; set; }
    }
    public record DrawPolygonDto
    {
        public int? DrawId { get; set; }
        public int? DefenceAreaId { get; set; }
        [SugarColumn(Length = 1000)]
        public string? pointListLatLng { get; set; }
        public int? Status { get; set; }
        public int pointType { get; set; }
    }
}
