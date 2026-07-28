using System.ComponentModel.DataAnnotations;

namespace Lan.Dto
{
    public class BindDeviceDto
    {
        [Required(ErrorMessage = "DefenceAreaId不能为空")]
        public int DefenceAreaId { get; set; }
        public int[]? CameraIds { get; set; }
        public int[]? RadarIds { get; set; }
    }
}
