using SqlSugar;

namespace Model
{
    [SugarTable("camera")]
    public class CameraModel
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        [SugarColumn(Length = 100)]
        public string Name { get; set; }
        public int BindingAreaId { get; set; }
        [SugarColumn(Length = 100)]
        public string Ip { get; set; }
        public int Port { get; set; }
        [SugarColumn(Length = 100)]
        public string Username { get; set; }
        [SugarColumn(Length = 100)]
        public string Password { get; set; }
        public float CameraHeight { get; set; }
        public string? sys_dict_data_size { get; set; }
        public int TrackParid { get; set; }
        public int TrackMode { get; set; }
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


        [SugarColumn(Length = 100)]
        public string CameraURL { get; set; }
        public float MinZoomPan { get; set; }
        public float MinZoomTilt { get; set; }
        public float MaxZoomPan { get; set; }
        public float MaxZoomTilt { get; set; }
        public int Counterclockwise { get; set; }
        //public string InspectionID { get; set; }
        //public string InspectionName { get; set; }
        public int IsTrack { get; set; }

        public int CameraId { get; set; }
        public string SourceToken { get; set; }
        
    }
}
