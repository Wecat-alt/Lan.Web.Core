using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    [SugarTable("trackinfo")]
    public class TrackInfo
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int TrackId { get; set; }
        public int AlarmId { get; set; }
        public DateTime UpdateTime { get; set; }
        [SugarColumn(Length = 18, DecimalDigits = 2)]
        public double Lat { get; set; }
        public double Lng { get; set; }
        public int TargetId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public string RadarIp { get; set; }
        public int AreaId { get; set; }
        public string UpTime { get; set; }
    }

    public class TrackInfoInsert
    {
        public int AlarmId { get; set; }
        public DateTime UpdateTime { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public int TargetId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
    }
}
