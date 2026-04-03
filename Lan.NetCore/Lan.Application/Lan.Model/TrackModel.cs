using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    [SugarTable("track")]
    public class TrackModel
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public int AlarmID { get; set; }
        //public string pointList { get; set; }
        public int TargetId { get; set; }
        public float AxesX { get; set; }
        public float AxesY { get; set; }
        public float AzimuthAngle { get; set; }
        public float Distance { get; set; }
        public DateTime UpdateTime { get; set; } = DateTime.Now;
    }
}
