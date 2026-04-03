using CAT.NsrRadarSdk;
using CAT.NsrRadarSdk.NsrTypes;
using Model;
using System.Drawing;
using System.Net;

namespace Lan.ServiceCore.WebScoket
{
    public class WRadar
    {


        #region 成员变量
        NsrRadar _NsrRadar;

        int _nID;
        int _nDefenceAreaId;
        string _ip;
        int _nPort;
        float _fX;  //雷达相对防区的X偏移
        float _fY;  //雷达相对防区的Y偏移
        float _fTopMountingYOffset;     //顶装时，固定的Y坐标偏移量
        IPAddress _ipNetMask;
        IPAddress _ipGateway;
        RVS_DeviceAddress _devAddr;
        RVS_DeviceAddressNEW _devAddrnew;
        bool _bOnline;
        bool _bEnable;
        bool _bInvertX;
        int _nPriority;
        int _nHeartTime;
        float _fOrientation;
        PointF[] _ptsAlarmAreaVertices;
        bool _bInitialized;
        RVS_Target_List _RadarTargets;
        object _lockTargets;
        bool _bDefenceEnable;
        AutoResetEvent _evtReceivedReply;

        string _Latitude;
        string _Longitude;
        string _NorthDeviationAngle;
        int _DefenceRadius;
        int _DefenceAngle;
        int _Status;
        #endregion

        #region 属性


        public NsrRadar C_NsrRadar
        {
            get { return _NsrRadar; }
            set { _NsrRadar = value; }
        }
        public int ID
        {
            get { return _nID; }
            internal set { _nID = value; }
        }

        public int DefenceAreaId
        {
            get { return _nDefenceAreaId; }
        }

        public bool DefenceEnable
        {
            get { return _bDefenceEnable; }
            set { _bDefenceEnable = value; }

        }

        public IPEndPoint RadarEndPoint
        {
            get { return new IPEndPoint(IPAddress.Parse(_ip), _nPort); }
            internal set
            {
                _ip = value.Address.ToString();
                _nPort = value.Port;
            }
        }
        public string Ip
        {
            get { return _ip; }
            set { _ip = value; }
        }
        /// <summary>
        /// 雷达相对防区的y偏移
        /// </summary>
        public float Y
        {
            get { return _fY; }
            set { _fY = value; }
        }
        /// <summary>
        ///雷达相对防区的X偏移
        /// </summary>
        public float X
        {
            get { return _fX; }
            set { _fX = value; }
        }

        /// <summary> 顶装时，固定的Y坐标偏移量
        /// 顶装时，固定的Y坐标偏移量
        /// </summary>
        public float TopMountingYOffset
        {
            get { return _fTopMountingYOffset; }
            private set { _fTopMountingYOffset = value; }
        }

        public int Port
        {
            get { return _nPort; }
            set { _nPort = value; }
        }

        public IPAddress NetMask
        {
            get { return _ipNetMask; }
            private set { _ipNetMask = value; }
        }

        public IPAddress Gateway
        {
            get { return _ipGateway; }
            private set { _ipGateway = value; }
        }

        public RVS_DeviceAddress DevAddress
        {
            get { return _devAddr; }
            set { _devAddr = value; }
        }
        public RVS_DeviceAddressNEW DevAddrnewnew
        {
            get { return _devAddrnew; }
            internal set { _devAddrnew = value; }
        }

        public bool Online
        {
            get
            {
                if (_NsrRadar != null)
                {
                    return _NsrRadar.Online;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (_NsrRadar != null)
                {
                    _bOnline = _NsrRadar.Online;
                }
                else
                {
                    _bOnline = false;
                }
            }
        }

        /// <summary>
        /// 是否解析数据
        /// </summary>
        public bool c_bReceiveData = false;
        /// <summary>
        /// 雷达同步时间
        /// </summary>
        public DateTime SetTime { get; set; }
        public bool InvertX
        {
            get { return _bInvertX; }
            set { _bInvertX = value; }
        }

        public bool Enable
        {
            get { return _bEnable; }
            set { _bEnable = value; }
        }

        public int Priority
        {
            get { return _nPriority; }
            set { _nPriority = value; }
        }

        public int HeartTime
        {
            get { return _nHeartTime; }
            set { _nHeartTime = value; }
        }

        public float Orientation
        {
            get { return _fOrientation; }
            set { _fOrientation = value; }
        }

        public PointF[] PtsAlarmAreaVertices
        {
            get { return _ptsAlarmAreaVertices; }
            set { _ptsAlarmAreaVertices = value; }
        }

        //public bool Buzzer
        //{
        //    get { return _bBuzzer; }
        //}

        public string FirmwareVersion { get; private set; }

        public string AlgorithmVersion { get; private set; }

        public bool Initialized
        {
            get { return _bInitialized; }
            private set { _bInitialized = value; }
        }

        public object ReceivedFrame { get; set; }

        public RVS_Target_List RadarTargets
        {
            get
            {
                lock (_lockTargets)
                    return _RadarTargets;
            }
            set
            {
                lock (_lockTargets)
                    _RadarTargets = value;
            }
        }

        public string Latitude
        {
            get { return _Latitude; }
            set { _Latitude = value; }
        }

        public string Longitude
        {
            get { return _Longitude; }
            set { _Longitude = value; }
        }

        public string NorthDeviationAngle
        {
            get { return _NorthDeviationAngle; }
            set { _NorthDeviationAngle = value; }
        }
        public int DefenceRadius
        {
            get { return _DefenceRadius; }
            set { _DefenceRadius = value; }
        }
        public int DefenceAngle
        {
            get { return _DefenceAngle; }
            set { _DefenceAngle = value; }
        }
        public int Status
        {
            get { return _Status; }
            set { _Status = value; }
        }


        #endregion

        #region 获取雷达类型

        public enum RadarType
        {
            Unknown,
            SP100,
            SP200,
            SP100W,
            SP100W_M,
            SP50W,
            SP300W,
            SP300W_M,
            SP60W,
            SP120,
            Laser = 0x20,
        }


        public RadarType Type
        {
            get
            {
                RadarType t;
                switch (_devAddrnew)
                {
                    case RVS_DeviceAddressNEW.Unknown:
                        t = RadarType.Unknown;
                        break;
                    case RVS_DeviceAddressNEW.RADAR_ADDR:
                        t = RadarType.SP100;
                        break;
                    case RVS_DeviceAddressNEW.RADAR_ADDR_new:
                        t = RadarType.SP100;
                        break;

                    case RVS_DeviceAddressNEW.WVF_RADAR_100W_ADDR:
                        t = RadarType.SP100W;
                        break;
                    case RVS_DeviceAddressNEW.WVF_RADAR_100W_new_ADDR:
                        t = RadarType.SP100W;
                        break;
                    case RVS_DeviceAddressNEW.WVF_RADAR_NSR50W_ADDR:
                        t = RadarType.SP50W;
                        break;
                    case RVS_DeviceAddressNEW.WVF_RADAR_50W_new_ADDR:
                        t = RadarType.SP50W;
                        break;
                    case RVS_DeviceAddressNEW.LASER_ADDR:
                        t = RadarType.Laser;
                        break;
                    case RVS_DeviceAddressNEW.WVF_RADAR_300W_ADDR:
                        t = RadarType.SP300W;
                        break;
                    case RVS_DeviceAddressNEW.WVF_RADAR_300W_new_ADDR:
                        t = RadarType.SP300W;
                        break;
                    case RVS_DeviceAddressNEW.WVF_RADAR_300_M_ADDR:
                        t = RadarType.SP300W_M;
                        break;
                    case RVS_DeviceAddressNEW.WVF_RADAR_NSR200_ADDR:
                        t = RadarType.SP200;
                        break;
                    case RVS_DeviceAddressNEW.WVF_RADAR_NSR60W_ADDR:
                        t = RadarType.SP60W;
                        break;
                    case RVS_DeviceAddressNEW.WVF_RADAR_NSR120_ADDR:
                        t = RadarType.SP120;
                        break;
                    default:
                        t = RadarType.SP100W;
                        break;
                }
                return t;
            }
        }

        #endregion



        #region 构造函数和初始化ListRadarPolygon

        protected WRadar()
        {
            _nID = -1;
            _bOnline = false;
            _bEnable = false;
            _nPriority = 4;
            _nHeartTime = 30;
            _bInitialized = false;
            _bInvertX = false;
            _nDefenceAreaId = -1;
            _nPort = 8100;
            _ptsAlarmAreaVertices = new PointF[0];
            _lockTargets = new object();
            _RadarTargets = null;
            _devAddr = RVS_DeviceAddress.Unknown;
            _evtReceivedReply = new AutoResetEvent(false);
            _fTopMountingYOffset = -1;

            _Latitude = "0";
            _Longitude = "0";
            _NorthDeviationAngle = "0";
            _DefenceRadius = 0;
            _DefenceAngle = 0;
            _Status = 0;
        }

        protected WRadar(IPEndPoint epRadar, RVS_DeviceAddress devAddr)
            : this()
        {
            this.RadarEndPoint = epRadar;
            _devAddr = devAddr;

            _bInitialized = true;
            SaveToDatabase();
        }


        internal WRadar(IPEndPoint epRadar, int nDefenceAreaId, bool bEnable, int nPriority, float fOrientation, bool bInvertX, float f_x, float f_y, float fTopMountingYOffset)
            : this()
        {
            this.RadarEndPoint = epRadar;
            if (_nPort < 8000)
            {
                _devAddr = RVS_DeviceAddress.LASER_ADDR;
            }

            _nDefenceAreaId = nDefenceAreaId;
            _bEnable = bEnable;
            _nPriority = nPriority;
            _fOrientation = fOrientation;
            _bInvertX = bInvertX;
            _fX = f_x;
            _fY = f_y;
            _fTopMountingYOffset = fTopMountingYOffset;

            SaveToDatabase();

        }

        internal WRadar(RadarModel radar)
            : this()
        {


            _nID = radar.Id;
            _nDefenceAreaId = radar.BindingAreaId;
            _ip = radar.Ip;
            _nPort = radar.Port;
            _bEnable = radar.Status == 1 ? true : false;

            _Latitude = radar.Latitude;
            _Longitude = radar.Longitude;
            _NorthDeviationAngle = radar.NorthDeviationAngle;
            _DefenceRadius = radar.DefenceRadius;
            _DefenceAngle = radar.DefenceAngle;
            _Status = radar.Status;

        }

        internal WRadar Clone()
        {
            WRadar newRadar = new WRadar();
            newRadar.Ip = Ip;
            newRadar.DevAddress = DevAddress;
            //newRadar.DevRadarType = DevRadarType;
            newRadar._bOnline = _bOnline;  //不能使用Online属性，会错误的引发雷达连接事件
            return newRadar;
        }

        public PointF ConvertPoint(float[] sz)
        {
            double rad = sz[0] * Math.PI / 180.0;
            double x = sz[1] * Math.Sin(rad);
            double y = sz[1] * Math.Cos(rad);
            return new PointF((float)x, (float)y);
        }


        #endregion


        #region 通信协议封装
        public bool NetSet(IPAddress ip, IPAddress netmask, IPAddress gateway)
        {

            return _NsrRadar.SetIpAddress(ip, netmask, gateway);
        }



        public bool ReadStatus(ref rvs_PARAM_STATUS frame)
        {
            bool result = _NsrRadar.GetStatus(ref frame);

            if (result)
            {
                //if (ReceivedFrame is rvs_PARAM_STATUS)
                //{
                // frame = (rvs_PARAM_STATUS)ReceivedFrame;
                _nHeartTime = frame.heart.time;
                PointF[] pts = new PointF[frame.position.Length];
                int flag = 0;
                for (int i = 0; i < pts.Length; i++)
                {
                    float x = frame.position[i].X;
                    float y = frame.position[i].Y;
                    int index = frame.position[i].coordinateNo - 1;
                    if (index >= 0 && index < pts.Length)
                    {
                        pts[index] = new PointF(x, y);
                        flag |= (1 << index);
                    }
                }
                if (pts.Length == 4 && flag == 0x0F)
                    _ptsAlarmAreaVertices = pts;

                AlgorithmVersion = frame.radarVerInfo.AlgorithmVersion;
                FirmwareVersion = frame.radarVerInfo.FirmwareVersion;

                SaveToDatabase();
                //}
                //else
                //    result = false;
            }
            return result;
        }



        #endregion

        #region 雷达功能实现





        public bool BindToDefenceArea(int defenceAreaId)
        {
            _nDefenceAreaId = defenceAreaId;
            return SaveToDatabase();
        }

        public bool UnBindToDefenceArea()
        {
            return BindToDefenceArea(-1);
        }

        public bool SetAlarmAreaVertices(int index, PointF newPoint)
        {
            throw new NotImplementedException();
        }

        public void SetOnline(bool newState)
        {
            //_bOnline = newState;
            Online = newState;
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 保存到数据库（包含添加和修改功能）
        /// </summary>
        /// <returns>执行成功返回true</returns>
        public bool SaveToDatabase()
        {
            //if (_nID > 0 && RadarDatabase.Exist(this))
            //    return RadarDatabase.UpdateRadarBy(_nID, this);
            //RadarDatabase.DeleteRadarBy(_ip);
            //return RadarDatabase.AddRadar(this);
            return true;
        }

        #endregion





    }
}
