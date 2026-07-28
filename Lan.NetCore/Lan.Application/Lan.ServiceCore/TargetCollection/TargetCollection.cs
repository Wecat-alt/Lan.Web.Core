using Lan.Infrastructure.Geometries;
using Lan.ServiceCore.Public;
using Lan.ServiceCore.Signalr;
using Lan.ServiceCore.WebScoket;
using NetTopologySuite.Geometries;
using System.Collections.Concurrent;

namespace Lan.ServiceCore.TargetCollection
{
    public class TargetCollection
    {
        public enum TrackModeEnum
        {
            Auto,
            UserSelect,
            Manual
        }

        TrackModeEnum _trackMode;

        List<TargetPath> _targetPathList;       //防区当前报警目标列表

        public List<TargetPath> _targetPathListqt;       //防区当前报警目标列表
        int _pathId = 0;                        //在一个防区内，报警目标的唯一标志，非负整数
        int _pathIdSelected1 = -1;               //当前选定的跟踪目标标志，负数表示没有选中
        List<int> _pathIdSelected = new List<int>();               //当前选定的跟踪目标标志，负数表示没有选中
        DateTime _trackTime;                    //当前目标轮询的起始时间点，用于判断是否切换到下一个目标
        ConcurrentQueue<RadarTargetItem> _radarTargetQueue;    //雷达目标缓存队列
        //List<TargetFuse> _dictTargetFuse;
        private WDefenceArea _defenceArea;

        /// <summary>
        /// 目标轨迹列表
        /// </summary>
        public List<TargetPath> TargetList
        {
            get { return _targetPathList; }
            set { _targetPathList = value; }
        }

        public List<int> TargetIdSelected
        {
            get { return _pathIdSelected; }
            set
            {
                _trackTime = DateTime.Now;
                _pathIdSelected = value;
            }
        }

        public WDefenceArea DefenceArea
        {
            get { return _defenceArea; }
        }

        public TargetCollection(WDefenceArea defenceArea)
        {
            _trackTime = DateTime.MinValue;
            _trackMode = TrackModeEnum.Auto;
            _targetPathList = new List<TargetPath>();
            _targetPathListqt = new List<TargetPath>();
            _radarTargetQueue = new ConcurrentQueue<RadarTargetItem>();
            //_dictTargetFuse = new List<TargetFuse>();
            _defenceArea = defenceArea;
        }



        /// <summary>
        /// 添加雷达的报警信息到目标缓存队列。
        /// 四层管线：QuickFilter → Transform → PolygonFilter → Dispatch
        /// </summary>
        internal bool AddTarget(WRadar radar, List<Coordinate[]> ListRadarPolygon1,
            List<Coordinate[]> ListRadarPolygon2, List<Coordinate[]> ListRadarPolygon3)
        {
            bool isAdded = false;
            var radarTacks = new List<SendMS>();
            const int AlarmId = -1;

            // ═══════ 批次级常量预计算（原来每个 target 都算一遍） ═══════
            double radarLat = Convert.ToDouble(radar.Latitude);
            double radarLon = Convert.ToDouble(radar.Longitude);
            double northDev = double.Parse(radar.NorthDeviationAngle);
            double latRad   = radarLat * Math.PI / 180.0;
            double lonRad   = radarLon * Math.PI / 180.0;
            double cosLat   = Math.Cos(latRad);
            double sinLat   = Math.Sin(latRad);
            float  halfAngle = radar.DefenceAngle / 2f;
            float  defRadius = DefenceArea.DefenceRadius;
            float  rdrRadius = radar.DefenceRadius;
            // 预筛安全边距：rotation 保距，translation 偏移 ≤ √(X²+Y²) ≤ 10m
            const float preFilterMargin = 10f;
            float  preFilterMaxDist = Math.Min(defRadius, rdrRadius) + preFilterMargin;

            foreach (var tar in radar.RadarTargets.Targets)
            {
                // ── Stage 1: QuickFilter（廉价拒绝，避免昂贵的 RadarTargetItem 构造） ──
                if (!PassQuickFilter(tar, halfAngle, preFilterMaxDist))
                    continue;

                DateTime now = DateTime.Now;
                string utime = now.ToString("yyyy-MM-dd HH:mm:ss:ffffff");

                // ── Stage 2: Transform（昂贵运算，只对通过预筛的目标执行） ──
                float x = radar.InvertX ? -tar.X : tar.X;
                var tarItem = new RadarTargetItem(x, tar.Y, tar.SpeedY, tar.SpeedX,
                    now, radar, tar.Type, tar.AxesX, tar.AxesY, tar.AzimuthAngle, tar.Id);
                tarItem.TargetId = (int)tar.Id;

                // 精确距离过滤（与原来逻辑完全相同）
                if (tarItem.Distance > defRadius || tarItem.Distance > rdrRadius)
                    continue;

                var (lat, lng) = TransformLatLon(
                    latRad, lonRad, cosLat, sinLat, northDev,
                    tarItem.AxesX, tarItem.AxesY, tarItem.AzimuthAngle);

                // ── Stage 3: PolygonFilter（多边形判读，逻辑不变） ──
                if (!PassPolygonFilter(lat, lng, ListRadarPolygon1, ListRadarPolygon2))
                    continue;

                // ── Stage 4: Dispatch（构造消息、写入下游队列） ──
                lat = Math.Round(lat, 6);
                lng = Math.Round(lng, 6);

                radarTacks.Add(DispatchTarget(
                    radar, tar, tarItem, lat, lng, now, utime, AlarmId, DefenceArea.ID));
                isAdded = true;
            }

            WDefenceArea.AddTarget(radarTacks);
            return isAdded;
        }

        // ═══════════════ Stage 1: QuickFilter ═══════════════

        /// <summary>廉价预筛：角度过滤 + 保守距离过滤。拒绝大部分目标，避免后续昂贵运算。</summary>
        private static bool PassQuickFilter(IRvs_Target tar, float halfAngle, float maxDist)
        {
            if (Math.Abs(tar.AzimuthAngle) > halfAngle)
                return false;

            float rawDist = MathF.Sqrt(tar.AxesX * tar.AxesX + tar.AxesY * tar.AxesY);
            return rawDist <= maxDist;
        }

        // ═══════════════ Stage 2: Transform ═══════════════

        /// <summary>
        /// 雷达坐标 → 经纬度（数学等价于原 Share.GetLatLon，但使用批次级预计算常量）。
        /// bearingRad = NorthDeviationAngle - (-AzimuthAngle) = NorthDev + Azimuth
        /// </summary>
        private static (double Lat, double Lng) TransformLatLon(
            double latRad, double lonRad, double cosLat, double sinLat, double northDev,
            float axesX, float axesY, float azimuthAngle)
        {
            const double earthRadius = 6371393.0;

            double dist = Math.Sqrt(axesX * axesX + axesY * axesY);
            if (dist <= 0)
                return (latRad * 180.0 / Math.PI, lonRad * 180.0 / Math.PI);

            double bearingRad = (northDev + azimuthAngle) * Math.PI / 180.0;
            double angularDist = dist / earthRadius;

            double newLatRad = Math.Asin(
                sinLat * Math.Cos(angularDist) +
                cosLat * Math.Sin(angularDist) * Math.Cos(bearingRad));

            double newLonRad = lonRad + Math.Atan2(
                Math.Sin(bearingRad) * Math.Sin(angularDist) * cosLat,
                Math.Cos(angularDist) - sinLat * Math.Sin(newLatRad));

            return (newLatRad * 180.0 / Math.PI, newLonRad * 180.0 / Math.PI);
        }

        // ═══════════════ Stage 3: PolygonFilter ═══════════════

        /// <summary>多边形判读（与原来 TrimDrawpolygon 逻辑完全一致）</summary>
        private static bool PassPolygonFilter(double lat, double lng,
            List<Coordinate[]> includePolys, List<Coordinate[]> excludePolys)
        {
            // 包含区：目标必须在区域内
            if (includePolys.Count > 0 && !IsPointInAnyPolygon(lat, lng, includePolys))
                return false;

            // 排除区：目标不能在区域内
            if (IsPointInAnyPolygon(lat, lng, excludePolys))
                return false;

            return true;
        }

        private static bool IsPointInAnyPolygon(double lat, double lng, List<Coordinate[]> polygons)
        {
            if (polygons == null || polygons.Count == 0)
                return false;

            var geoService = new GeoService();
            var point = new Coordinate(lat, lng);

            foreach (var coords in polygons)
            {
                if (geoService.IsPointInPolygon(point, coords))
                    return true;
            }
            return false;
        }

        // ═══════════════ Stage 4: Dispatch ═══════════════

        /// <summary>构造消息并写入下游队列（与原来逻辑完全一致）</summary>
        private static SendMS DispatchTarget(WRadar radar, IRvs_Target tar, RadarTargetItem tarItem,
            double lat, double lng, DateTime now, string utime, int alarmId, int areaId)
        {
            // SendMS → Worker (SignalR 实时推送)
            var sms = new SendMS
            {
                TargetId             = tarItem.TargetId,
                TargetType           = (int)tar.Type,
                SpeedX               = tar.SpeedX.ToString(),
                SpeedY               = tar.SpeedY.ToString(),
                Lat                  = lat,
                Lng                  = lng,
                DateTime             = tarItem.UpdateTime.ToString("mmss"),
                Distance             = tar.Distance.ToString(),
                AzimuthAngle         = tar.AzimuthAngle.ToString(),
                NorthDeviationAngle  = radar.NorthDeviationAngle,
                RadarIp              = radar.Ip,
                AxesX                = tar.AxesX,
                AxesY                = tar.AxesY,
                AxesZ                = tar.AxesZ,
                AreaId               = areaId
            };
            Worker.AddTarget(sms);

            // TrackInfo → RadarDataChannelService (轨迹写入数据库)
            var trackInfo = new TrackInfo
            {
                AlarmId    = alarmId,
                UpdateTime = tarItem.UpdateTime,
                Lat        = lat,
                Lng        = lng,
                TargetId   = tarItem.TargetId,
                X          = tar.X,
                Y          = tar.Y,
                RadarIp    = radar.Ip,
                AreaId     = areaId,
                UpTime     = utime
            };

            if (RadarDataChannelService.Instance != null)
            {
                RadarDataChannelService.Instance.Write(trackInfo);
            }
            else
            {
                Console.WriteLine("RadarDataChannelService 未初始化");
            }

            // AlarmEvent → AlarmBackgroundService (报警队列)
            if (AlarmBackgroundService.Instance != null)
            {
                var alarmEvent = new AlarmEvent
                {
                    ZoneId    = tarItem.DefenceId.ToString(),
                    AlarmTime = now,
                    TargetId  = tarItem.TargetId,
                    RadarIp   = radar.Ip
                };
                AlarmBackgroundService.Instance.Write(alarmEvent);
            }
            else
            {
                Console.WriteLine("AlarmBackgroundService 未初始化");
            }

            return sms;
        }

    }
    public class SendMS
    {
        public int TargetId { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public int TargetType { get; set; }
        public string DateTime { get; set; }
        public string Distance { get; set; }
        public string AzimuthAngle { get; set; }
        public string SpeedX { get; set; }
        public string SpeedY { get; set; }
        public string NorthDeviationAngle { get; set; }
        public string RadarIp { get; set; }
        public float AxesX { get; set; }
        public float AxesY { get; set; }
        public float AxesZ { get; set; }
        public int AreaId { get; set; }
    }

    public class AlarmEvent
    {
        public string ZoneId { get; set; }
        public DateTime AlarmTime { get; set; }
        public int TargetId { get; set; }
        public string RadarIp { get; set; }
    }

}
