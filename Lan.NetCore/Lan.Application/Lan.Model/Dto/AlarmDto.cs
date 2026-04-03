using Lan.Model;

namespace Lan.Dto
{
    /// <summary>
    /// 查询对象
    /// </summary>
    public class AlarmQueryDto : PagerInfo
    {
        public string? ip { get; set; }
        public int? AreaId { get; set; }

        public string? StartTime { get; set; }
        public string? EndTime { get; set; }

    }
    public class AlarmDto
    {
        public int Id { get; set; }
        public int AreaId { get; set; }
        public string AreaName { get; set; }
        public DateTime DateTime { get; set; } = DateTime.Now;
        public string DealWith { get; set; }
        public string VideoName { get; set; }
        public string CameraIp { get; set; }
        public string Level { get; set; }

        public float Latitude { get; set; }
        public float Longitude { get; set; }
    }
}
