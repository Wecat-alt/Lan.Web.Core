using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    [SugarTable("calibration")]
    public class Calibration
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public string CameraIp { get; set; }
        public int DefenceareaId { get; set; }
        public float CalibrationDistance { get; set; }
        public float CameraPointX { get; set; }
        public float CameraPointY { get; set; }
        public float CameraHeight { get; set; }
        public float CamerarPointAngle { get; set; }
        

       
    }
}
