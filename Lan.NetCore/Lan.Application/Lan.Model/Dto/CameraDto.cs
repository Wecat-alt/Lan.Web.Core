using Lan.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Dto
{
    public class CameraQueryDto : PagerInfo
    {
        public string? ip { get; set; }
        public int? defenceareaId { get; set; }
    }
    public class CameraQueryDto2
    {
        public int BindingAreaId { get; set; }
        public int[]? CameraIds { get; set; }
    }
    public class CameraQueryDto1
    {
        public int[]? CameraIds { get; set; }
    }

    public class CameraDto
    {
        [Required(ErrorMessage = "Id不能为空")]
        public int Id { get; set; }
        public string Name { get; set; }
        public int? BindingAreaId { get; set; }
        [Required(ErrorMessage = "Ip不能为空")]
        public string? Ip { get; set; }
        public int Port { get; set; }
        [Required(ErrorMessage = "Username不能为空")]
        public string Username { get; set; }
        [Required(ErrorMessage = "Password不能为空")]
        public string Password { get; set; }
        public float CameraHeight { get; set; }
        public string? sys_dict_data_size { get; set; }
        public int? Trackparid { get; set; }
        public int? TrackMode { get; set; }
        public float MinViewAngle { get; set; }
        public float ViewAngle2X { get; set; }
        public float MaxViewAngle { get; set; }
        public int? MaxZoom { get; set; }
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


        public string CameraURL { get; set; }
        public float MinZoomPan { get; set; }
        public float MinZoomTilt { get; set; }
        public float MaxZoomPan { get; set; }
        public float MaxZoomTilt { get; set; }
        public int? Counterclockwise { get; set; }
        //public string? InspectionID { get; set; }
        //public string? InspectionName { get; set; }
        public int IsTrack { get; set; }

        public int CameraId { get; set; }
        public string SourceToken { get; set; }
        public string ZoneName { get; set; }
    }
}