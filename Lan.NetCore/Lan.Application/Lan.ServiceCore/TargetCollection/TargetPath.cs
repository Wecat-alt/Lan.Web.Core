using CAT.NsrRadarSdk.NsrTypes;
using Lan.ServiceCore.WebScoket;
using System.Drawing;

namespace Lan.ServiceCore.TargetCollection
{
    public enum PathState
    {
        /// <summary>
        /// 新建目标
        /// </summary>
        New,
        /// <summary>
        /// 报警目标
        /// </summary>
        Enable,
        /// <summary>
        /// 丢失的目标
        /// </summary>
        Lost,
        /// <summary>
        /// 误报
        /// </summary>
        FalseAlarm,

    }

    public class TargetPath : ICollection<RadarTargetItem>
    {


        #region 成员变量

        #region 报警目标闪烁颜色
        static Color[] TargetTwinkleColors = new Color[]{
                //Color.FromArgb(0,0,0),
                //Color.FromArgb(21,21,0),
                //Color.FromArgb(43,43,0),
                Color.FromArgb(64,64,0),
                Color.FromArgb(85,85,0),
                Color.FromArgb(106,106,0),
                Color.FromArgb(128,128,0),
                Color.FromArgb(149,149,0),
                Color.FromArgb(170,170,0),
                Color.FromArgb(191,191,0),
                //Color.FromArgb(213,213,0),
                Color.FromArgb(234,234,0),
                Color.FromArgb(255,255,0),
                //Color.FromArgb(255,255,21),
                Color.FromArgb(255,255,43),
                //Color.FromArgb(255,255,64),
                Color.FromArgb(255,255,85),
                //Color.FromArgb(255,255,106),
                Color.FromArgb(255,255,128),
                //Color.FromArgb(255,255,149),
                Color.FromArgb(255,255,170),
                //Color.FromArgb(255,255,191),
                Color.FromArgb(255,255,213),
                //Color.FromArgb(255,255,234),
                //Color.FromArgb(255,255,255),
            };

        #endregion
        /// <summary>
        /// 报警颜色
        /// </summary>
        static Color ColorAlarm = Color.FromArgb(0xe9, 0xff, 0x2b);
        /// <summary>
        /// 预警颜色
        /// </summary>
        static Color ColorLost = Color.FromArgb(171, 189, 13);
        /// <summary>
        /// 预警轨迹
        /// </summary>
        static Color ColorNormalWave = Color.FromArgb(0, 0xFF, 0x43);//预警轨迹
        private static Color ColorAlarmTracking = Color.Red;
        private static Color ColorLostTracking = Color.DarkRed;

        int _nTargetLight, _nStep;

        /// <summary>
        /// 预警
        /// </summary>
        public bool IsEarlyWarning = false;

        List<RadarTargetItem> _listItems;
        private TargetCollection _parent;
        private WRadar _radar;

        #endregion

        #region 属性

        /// <summary>
        /// 报警目标唯一识别号
        /// </summary>
        public int TargetId { get; set; }

        /// <summary>
        /// 目标角度
        /// </summary>
        public float Angle
        {
            get
            {
                return _listItems.Count == 0 ?
                    0 :
                    _listItems.Last().Angle;
            }
        }
        public PointF pt1
        {
            get
            {
                return _listItems.Count == 0 ?
                   new PointF(0, 0) :
                    _listItems.Last().ConvertPoint();
            }
        }
        /// <summary>
        /// 球机跟踪后保存坐标
        /// </summary>
        public PointF lastpt1
        {
            get;
            set;

        }
        /// <summary>
        /// 轨迹被球机跟踪第一次跟踪
        /// </summary>
        public bool IsCamParamsPtrs = true;

        /// <summary>
        /// 球机跟踪后保存目标距离
        /// </summary>
        public float lastDistance
        {
            get;
            set;

        }
        /// <summary>
        /// 球机跟踪后保存角度
        /// </summary>
        public float lastAngle
        {
            get;
            set;
        }
        /// <summary>
        /// 目标距离
        /// </summary>
        public float Distance
        {
            get
            {
                return _listItems.Count == 0 ?
                    0 :
                    _listItems.Last().Distance;
            }
        }
        /// <summary>
        /// 目标速度
        /// </summary>
        public float SpeedY
        {
            get
            {
                return _listItems.Count == 0 ?
                    0 :
                    _listItems.Last().SpeedY;
            }
        }

        public float SpeedX
        {
            get
            {
                return _listItems.Count == 0 ?
                    0 :
                    _listItems.Last().SpeedX;
            }
        }

        public int DefenceId
        {
            get
            {
                return _listItems.Count == 0 ?
                    0 :
                    _listItems.Last().DefenceId;
            }
        }

        public string RadarIP
        {
            get
            {
                return _listItems.Count == 0 ?
                    "0" :
                    _listItems.Last().RadarIP;
            }
        }

        public string Latitude
        {
            get
            {
                return _listItems.Count == 0 ? "0" : _listItems.Last().Latitude;
            }
        }
        public string Longitude
        {
            get
            {
                return _listItems.Count == 0 ? "0" : _listItems.Last().Longitude;
            }
        }
        public string NorthDeviationAngle
        {
            get
            {
                return _listItems.Count == 0 ? "0" : _listItems.Last().NorthDeviationAngle;
            }
        }

        public string MainRadarIP
        {
            get
            {
                return _listItems.Count == 0 ?
                    "0" :
                    _listItems.Last().MainRadarIP;
            }
        }

        /// <summary>
        /// 雷达旋转角度
        /// </summary>
        public float RadarOrientation
        {
            get
            {
                return _listItems.Count == 0 ?
                    0 :
                    _listItems.Last().RadarOrientation;
            }
        }
        public float AxesX
        {
            get
            {
                return _listItems.Count == 0 ?
                    0 :
                    _listItems.Last().AxesX;
            }
        }
        public float AxesY
        {
            get
            {
                return _listItems.Count == 0 ?
                    0 :
                    _listItems.Last().AxesY;
            }
        }

        public float NewX
        {
            get
            {
                return _listItems.Count == 0 ?
                    0 :
                    _listItems.Last().NewX;
            }
        }
        public float NewY
        {
            get
            {
                return _listItems.Count == 0 ?
                    0 :
                    _listItems.Last().NewY;
            }
        }

        /// <summary>
        /// x方向速度
        /// </summary>
        public float NewSpeed_vx
        {
            get;
            set;
        }
        /// <summary>
        /// y方向速度
        /// </summary>
        public float NewSpeed_vy
        {
            get;
            set;
        }



        public float AzimuthAngle
        {
            get
            {
                return _listItems.Count == 0 ?
                    0 :
                    _listItems.Last().AzimuthAngle;
            }
        }



        /// <summary>
        /// 轨迹更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 雷达IP
        /// </summary>

        /// <summary>
        /// 目标出现时间
        /// </summary>
        public DateTime AppearTime { get; set; }



        public int Count { get { return _listItems.Count; } }

        public PathState State { get; set; }


        /// <summary>
        /// 是否已经使用最新的坐标控制过球机
        /// </summary>
        public bool AlreadyTracked { get; set; }

        /// <summary>
        /// 目标类型 车2 人1 树3 无法识别0
        /// </summary>
        public uint TargetType { get; set; }

        public string TargetTypeName
        {
            get
            {
                switch (TargetType)
                {
                    case 2:
                        return "车";
                    case 1:
                        return "人";
                    case 3:
                        return "树";
                    default:
                        return "未知";

                }
            }
        }

        #endregion

        #region 方法





        #endregion


        #region  ICollection<RadarTargetItem>接口实现
        public void Add(RadarTargetItem item)
        {
            TargetType = item.TargetType;
            _listItems.Add(item);
            //if (_listItems.Count > 6)
            //{
            //    formTest2D();
            //}
            UpdateTime = item.UpdateTime;
            AlreadyTracked = false;

        }

        public void Clear()
        {
            _listItems.Clear();
        }

        public bool Contains(RadarTargetItem item)
        {
            return _listItems.Contains(item);
        }

        public void CopyTo(RadarTargetItem[] array, int arrayIndex)
        {
            _listItems.CopyTo(array, arrayIndex);
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(RadarTargetItem item)
        {
            return _listItems.Remove(item);
        }

        public IEnumerator<RadarTargetItem> GetEnumerator()
        {
            return _listItems.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _listItems.GetEnumerator();
        }

        #endregion


    }
}
