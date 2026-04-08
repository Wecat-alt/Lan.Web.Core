using Dm.util;
using Lan.Infrastructure.Geometries;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.Public;
using Lan.ServiceCore.Services;
using Lan.ServiceCore.Signalr;
using Lan.ServiceCore.WebScoket;
using Lan.Shared;
using Model;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.HPRtree;
using NetTopologySuite.Operation.Distance;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Concurrent;
using System.Drawing;

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

        AlarmService alarmService = new AlarmService();
        TrackInfoService trackInfoService = new TrackInfoService();



        /// <summary>
        /// 添加雷达的报警信息到目标缓存队列
        /// </summary>
        /// <param name="radar">发生报警的雷达</param>
        /// <returns>是否成功添加了目标</returns>
        internal bool AddTarget(WRadar radar, List<Coordinate[]> ListRadarPolygon1, List<Coordinate[]> ListRadarPolygon2, List<Coordinate[]> ListRadarPolygon3)
        {
            DateTime now = DateTime.Now;
            bool isAdded = false;
            List<SendMS> radarTacks = new List<SendMS>();
            var AlarmId = -1;// alarmService.GetInfos(DefenceArea.ID);

            //var northDeviation = double.Parse(radar.NorthDeviationAngle);
            var radarLat = Convert.ToDouble(radar.Latitude);
            var radarLon = Convert.ToDouble(radar.Longitude);
            var halfDefenceAngle = radar.DefenceAngle / 2f;
            //var defenceRadius = DefenceArea.DefenceRadius;
            //var radarDefenceRadius = radar.DefenceRadius;
            //var areaId = DefenceArea.ID;

            foreach (var tar in radar.RadarTargets.Targets)
            {
                float x = tar.X;

                if (radar.InvertX)  //判断是否需要对X坐标反向
                    x = -x;


                var ss = Math.Sqrt(tar.AxesX * tar.AxesX + tar.AxesY * tar.AxesY);

                string utime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ffffff");
                RadarTargetItem tarItem = new RadarTargetItem(x, tar.Y, tar.SpeedY, tar.SpeedX, now, radar, tar.Type, tar.AxesX, tar.AxesY, tar.AzimuthAngle, tar.Id);

                tarItem.TargetId = (int)tar.Id;
                if (tarItem.Distance > DefenceArea.DefenceRadius)
                {
                    //距离超过防区半径的目标，剔除
                    continue;
                }
                if (tarItem.Distance > radar.DefenceRadius)
                {
                    //距离超过防区半径的目标，剔除
                    continue;
                }

                if (Math.Abs(tar.AzimuthAngle) > halfDefenceAngle)
                {
                    continue;
                }
                float B_AzimuthAngle = float.Parse(radar.NorthDeviationAngle);
                Double[] AlarmLonLat = Share.GetLatLon(radarLat, radarLon, double.Parse(B_AzimuthAngle.ToString()), tarItem.AxesX, tarItem.AxesY, tarItem.AzimuthAngle);

                if (ListRadarPolygon1.Count > 0)
                {
                    if (!TrimDrawpolygon(AlarmLonLat, ListRadarPolygon1))
                    {
                        continue;
                    }
                }
                if (TrimDrawpolygon(AlarmLonLat, ListRadarPolygon2))
                {
                    continue;
                }

                var lat = Math.Round(AlarmLonLat[0], 6);
                var lng = Math.Round(AlarmLonLat[1], 6);

                SendMS sms = new SendMS { };
                sms.TargetId = tarItem.TargetId;
                sms.TargetType = (int)tar.Type;
                sms.SpeedX = tar.SpeedX.ToString();
                sms.SpeedY = tar.SpeedY.ToString();
                sms.Lat = lat;
                sms.Lng = lng;
                sms.DateTime = tarItem.UpdateTime.ToString("mmss");
                sms.Distance = tar.Distance.ToString();
                sms.AzimuthAngle = tar.AzimuthAngle.ToString();
                sms.NorthDeviationAngle = radar.NorthDeviationAngle;
                sms.RadarIp = radar.Ip;
                sms.AxesX = tar.AxesX;
                sms.AxesY = tar.AxesY;
                sms.AreaId = DefenceArea.ID;
                radarTacks.Add(sms);
                Worker.AddTarget(sms);


                TrackInfo trackInfo = new TrackInfo();
                trackInfo.AlarmId = AlarmId;
                trackInfo.UpdateTime = tarItem.UpdateTime;
                trackInfo.Lat = lat;
                trackInfo.Lng = lng;
                trackInfo.TargetId = tarItem.TargetId;
                trackInfo.X = tar.X;
                trackInfo.Y = tar.Y;
                trackInfo.RadarIp = radar.Ip;
                trackInfo.AreaId = DefenceArea.ID;
                trackInfo.UpTime = utime;

                // 添加到报警队列（非阻塞操作）
                if (AlarmBackgroundService.Instance != null)
                {
                    var alarmEvent = new AlarmEvent
                    {
                        ZoneId = tarItem.DefenceId.ToString(),
                        AlarmTime = now,
                        TargetId = tarItem.TargetId,
                        RadarIp = radar.Ip
                    };

                    AlarmBackgroundService.Instance.Write(alarmEvent);
                }
                else
                {
                    // 如果服务未启动，记录日志或采取其他措施
                    Console.WriteLine("AlarmBackgroundService 未初始化");
                }

                // 使用静态实例写入Channel
                if (RadarDataChannelService.Instance != null)
                {
                    RadarDataChannelService.Instance.Write(trackInfo);
                }
                else
                {
                    // 如果服务未启动，记录日志或采取其他措施
                    Console.WriteLine("RadarDataChannelService 未初始化");
                }

                //_radarTargetQueue.Enqueue(tarItem);
                isAdded = true;
            }
            WDefenceArea.AddTarget(radarTacks);

            return isAdded;
        }

        private bool TrimDrawpolygon(Double[] AlarmLonLat, List<Coordinate[]> ListRadarPolygon)
        {
            var geoService = new GeoService();

            if (ListRadarPolygon == null)
                return true;

            bool inPolygon = false;
            if (ListRadarPolygon.Count > 0)
            {
                foreach (var coordinates in ListRadarPolygon)
                {

                    Coordinate cc = new Coordinate(AlarmLonLat[0], AlarmLonLat[1]);
                    inPolygon = geoService.IsPointInPolygon(cc, coordinates);
                    if (inPolygon)
                    {
                        return inPolygon;
                    }
                }
            }
            return inPolygon;
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
