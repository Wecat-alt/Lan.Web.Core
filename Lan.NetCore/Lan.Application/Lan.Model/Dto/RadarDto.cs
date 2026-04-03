using Lan.Model;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Dto
{
    public class RadarQueryDto : PagerInfo
    {
        public string? ip { get; set; }
    }
    public record RadarQueryDto2
    {
        public int BindingAreaId { get; set; }
        public int[]? RadarIds { get; set; }
    }
    public record RadarQueryDto1
    {
        public int[]? RadarIds { get; set; }
    }
    public record RadarDto
    {
        public int Id { get; set; }
        public int? BindingAreaId { get; set; }
        [SugarColumn(Length = 100)]
        public required  string Ip { get; set; }
        public int Port { get; set; }
        public int Status { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string NorthDeviationAngle { get; set; }
        //public string Create_user { get; set; }
        //public DateTime Create_time { get; set; }
        public int DefenceRadius { get; set; }
        public int DefenceAngle { get; set; }
        public string RadarType { get; set; }
        public int? DefenceEnable { get; set; }
        public string? CameraIp { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? CameraURL { get; set; }
    }
}