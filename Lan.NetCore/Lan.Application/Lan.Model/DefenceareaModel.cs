using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    [SugarTable("defencearea")]
    public class DefenceareaModel
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        [SugarColumn(Length = 100)]
        public string Name { get; set; }
        public int DefenceEnable { get; set; }
        [SugarColumn(Length = 100, ColumnDescription = "状态")]
        public string DefenceEnableName { get; set; }
        public int DefenceRadius { get; set; }

        public string Latitude { get; set; }
        public string Longitude { get; set; }



        [SugarColumn(IsIgnore = true)]
        public int[]? CameraIds { get; set; }
        [SugarColumn(IsIgnore = true)]
        public List<CameraModel> Cameras { get; set; }
        [SugarColumn(IsIgnore = true)]
        public int[]? RadarIds { get; set; }
        [SugarColumn(IsIgnore = true)]
        public List<RadarModel> Radars { get; set; }
    }
}
