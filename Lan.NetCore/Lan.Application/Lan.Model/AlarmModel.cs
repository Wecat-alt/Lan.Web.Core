using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    [SugarTable("alarm")]
    public class AlarmModel
    {
        /// <summary>
        /// Id 
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public int AreaId { get; set; }
        [SugarColumn(Length = 100)]
        public string AreaName { get; set; }
        public DateTime DateTime { get; set; }


        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string IsActive { get; set; } = "true";
        // 每15秒的记录时间戳
        public DateTime RecordTime { get; set; }

        [SugarColumn(IsIgnore = true)] // 不存到数据库，仅用于内存管理
        public DateTime LastRecordTime { get; set; }

        [SugarColumn(Length = 10, ColumnDescription = "是否处理")]
        public string DealWith { get; set; }
        [SugarColumn(Length = 1000, ColumnDescription = "路径")]
        public string VideoName { get; set; }
        public string CameraIp { get; set; }
        public string Level { get; set; }

        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string RadarIp { get; set; }

        [SugarColumn(IsIgnore = true)]
        public DateTime LastDataTime { get; set; }
    }
}
