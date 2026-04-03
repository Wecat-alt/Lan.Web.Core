using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Model
{
    [SugarTable("trackparameter")]
    public class TrackParameter
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public int sys_dict_data { get; set; }
        [SugarColumn(Length = 255)]
        public string CameraName_cn { get; set; }
        [SugarColumn(Length = 255)]
        public string CameraName_en { get; set; }

        public float MinViewAngle { get; set; }
        public float ViewAngle2X { get; set; }
        public float MaxViewAngle { get; set; }
        public int MaxZoom { get; set; }
        public float MaxZoomRatio { get; set; }


        /// <summary>
        /// PanLeftToMiddle 
        /// </summary>
        public float PanLeft { get; set; }
        public float PanLeftAngle { get; set; }
        public float PanMiddle { get; set; }
        public float PanMiddleAngle { get; set; }

        /// <summary>
        /// PanMiddleToRight 
        /// </summary>
        public float PanMiddle2 { get; set; }
        public float PanMiddle2Angle { get; set; }
        public float PanRight { get; set; }
        public float PanRightAngle { get; set; }

        /// <summary>
        /// TiltTopToBottom 
        /// </summary>
        public float TiltTop { get; set; }
        public float TiltTopAngle { get; set; }
        public float TiltBottom { get; set; }
        public float TiltBottomAngle { get; set; }

        public float MinZoomPan { get; set; }
        public float MinZoomTilt { get; set; }
        public float MaxZoomPan { get; set; }
        public float MaxZoomTilt { get; set; }
        public int Counterclockwise { get; set; }
    }
}
