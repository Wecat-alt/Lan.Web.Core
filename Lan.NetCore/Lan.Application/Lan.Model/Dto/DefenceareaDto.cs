using Lan.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Dto
{
    public class DefenceareaQueryDto : PagerInfo
    {
        public string? name { get; set; }
    }
    public class DefenceareaDto
    {
        [Required(ErrorMessage = "Id不能为空")]
        public int Id { get; set; }
        public string Name { get; set; }
        public int DefenceEnable { get; set; }
        public string DefenceEnableName { get; set; }
        public int DefenceRadius { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public int[]? CameraIds { get; set; }
        public int[]? RadarIds { get; set; }
    }

    public class DefenceareaDtoParent
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long ParentId { get; set; } = 0;

        public List<DefenceareaDtoParent> children = new();
    }
}