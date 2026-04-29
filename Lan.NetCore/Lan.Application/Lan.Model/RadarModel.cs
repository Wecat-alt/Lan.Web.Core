using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    [SugarTable("radar")]
    public class RadarModel
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public int BindingAreaId { get; set; }
        [SugarColumn(Length = 100)]
        public string Ip { get; set; }
        public int Port { get; set; }
        public int Status { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string NorthDeviationAngle { get; set; }
        public string create_user { get; set; }
        public DateTime create_time { get; set; }
        public int DefenceRadius { get; set; } = 300;
        public int DefenceAngle { get; set; } = 90;
        public string RadarType { get; set; }
        [SugarColumn(IsIgnore = true)]
        public int? DefenceEnable { get; set; }
        [SugarColumn(IsIgnore = true)]
        public string? CameraIp { get; set; }
        [SugarColumn(IsIgnore = true)]
        public string? Username { get; set; }
        [SugarColumn(IsIgnore = true)]
        public string? Password { get; set; }
        [SugarColumn(IsIgnore = true)]
        public string? CameraURL { get; set; }
        [SugarColumn(IsIgnore = true)]
        public bool Online { get; set; }
    }

    public class UpdateRadarArea
    {
        public int BindingAreaId { get; set; }
        public string Ip { get; set; }
    }
}
