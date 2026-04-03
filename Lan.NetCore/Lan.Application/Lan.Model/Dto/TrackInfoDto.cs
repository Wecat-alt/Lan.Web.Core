using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Model.Dto
{
    public class TrackInfoQueryDto
    {
        public int AlarmId { get; set; }
        public int AreaId { get; set; }
        public string Time { get; set; }
    }
    public class TrackInfoDto
    {
        public int TrackId { get; set; }
        public int AlarmId { get; set; }
        public DateTime UpdateTime { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public int TargetId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
    }
}
