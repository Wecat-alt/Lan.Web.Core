using CAT.NsrRadarSdk.NsrTypes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CAT.NsrRadarSdk
{
    /// <summary>
    /// The nsr radar class, represent a true radar and implement its communication function.
    /// </summary>
    public class NsrRadar
    {
        #region 字段

        int _id;
        string _ip;
        int _port;

        RVS_DeviceAddress _devAddr;
        RVS_DeviceAddressNEW _devAddrNEW;
        bool _bOnline;
        long _nMissHeartCount;
        int _nHeartTime;
        bool _bBuzzer;
        ushort _uFirmwareVersion;
        ushort _uAlgorithmVersion;
        private Dictionary<RVS_COMMAND, AutoResetEvent> _replyEventDictionary;
        private Dictionary<RVS_COMMAND, rvs_FRAME_Context> _replyPacketDictionary;

        private PointF[] _ptsAlarmAreaVertices;

        #endregion

        #region 属性

        /// <summary>
        /// Radar id, not support now.
        /// </summary>
        internal int ID
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Get the radar's endpoint.
        /// </summary>
        public IPEndPoint RadarEndPoint
        {
            get { return new IPEndPoint(IPAddress.Parse(_ip), _port); }
            internal set
            {
                _ip = value.Address.ToString();
                _port = value.Port;
            }
        }

        /// <summary>
        /// Get radar's ip address.
        /// </summary>
        public string Ip
        {
            get { return _ip; }
            internal set { _ip = value; }
        }

        /// <summary>
        /// Count how many heart messages are lost.
        /// </summary>
        public long MissHeartCount
        {
            get { return Interlocked.Read(ref _nMissHeartCount); }
            internal set
            {
                Interlocked.Exchange(ref _nMissHeartCount, value);
                if (value >= _nHeartTime * NsrSdk.Instance.MissHeartTimeForOffline)
                {
                    Online = false;
                }
            }
        }

        /// <summary>
        /// Indicate the radar's online state.
        /// </summary>
        public bool Online
        {
            get { return _bOnline; }
            internal set
            {
                bool flag = _bOnline != value;
                if (value)
                    Interlocked.Exchange(ref _nMissHeartCount, 0);
                _bOnline = value;

                if (flag)
                {
                    NsrSdk.Instance.OnRadarOnlineStateChanged(this, value);
                }
            }
        }

        /// <summary>
        /// Get the radar's port.
        /// </summary>
        public int Port
        {
            get { return _port; }
            internal set { _port = value; }
        }

        /// <summary>
        /// Get the radar's mac address.
        /// </summary>
        public string MacAddress { get; internal set; }


        public string FirmwareVersion { get; internal set; }
        /// <summary>
        /// Get the radar's device address.
        /// </summary>
        public RVS_DeviceAddress DevAddress
        {
            get { return _devAddr; }
            internal set { _devAddr = value; }
        }
        public RVS_DeviceAddressNEW DevAddressNew
        {
            get { return _devAddrNEW; }
            internal set { _devAddrNEW = value; }
        }


        /// <summary>
        /// Get the radar's heart time.
        /// </summary>
        public int HeartTime
        {
            get { return _nHeartTime; }
            internal set { _nHeartTime = value; }
        }

        /// <summary>
        /// Get the radar's filter points.
        /// </summary>
        public PointF[] PtsAlarmAreaVertices
        {
            get { return _ptsAlarmAreaVertices; }
            internal set { _ptsAlarmAreaVertices = value; }
        }

        /// <summary>
        /// Get the radar's type.
        /// </summary>
        public RadarType Type
        {
            get
            {
                RadarType t;
                switch (_devAddrNEW)
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
                    case RVS_DeviceAddressNEW.WVF_RADAR_NSR150_ADDR:
                        t = RadarType.SP150;
                        break;
                    case RVS_DeviceAddressNEW.WVF_RADAR_NSR200_ADDR:
                        t = RadarType.SP200;
                        break;
                    case RVS_DeviceAddressNEW.WVF_RADAR_NSR120_ADDR:
                        t = RadarType.SP120;
                        break;
                    case RVS_DeviceAddressNEW.WVF_RADAR_NSR60W_ADDR:
                        t = RadarType.SP60W;
                        break;
                    case RVS_DeviceAddressNEW.WVF_RADAR_WTC261_3000_800_ADDR:
                        t = RadarType.SP800;
                        break;
                    default:
                        t = RadarType.SP100W;
                        break;
                }
                return t;
            }
        }

        #endregion

        #region 构造


        internal NsrRadar()
        {
            _replyEventDictionary = new Dictionary<RVS_COMMAND, AutoResetEvent>();
            _replyPacketDictionary = new Dictionary<RVS_COMMAND, rvs_FRAME_Context>();
            var cmds = new RVS_COMMAND[]
            {
                RVS_COMMAND.RVS_STATUSREADCOMMAND, RVS_COMMAND.RVS_BUZZERCOMMAND, RVS_COMMAND.RVS_HEARTBEATCOMMAND,
                RVS_COMMAND.RVS_COORDINATECOMMAND, RVS_COMMAND.RVS_SAVEPARACOMMAND, RVS_COMMAND.RVS_NETCOMMAND,
                RVS_COMMAND.RVS_HEARTCOMMAND, RVS_COMMAND.RVS_ANSWERCOMMAND,RVS_COMMAND.RVS_SYSTEMTIMECOMMAND
            };
            foreach (RVS_COMMAND rvsCommand in cmds)
            {
                _replyEventDictionary.Add(rvsCommand, new AutoResetEvent(false));
            }
        }

        ~NsrRadar()
        {
            var waithandles = _replyEventDictionary.Values.ToArray();
            _replyEventDictionary.Clear();
            foreach (AutoResetEvent autoResetEvent in waithandles)
            {
                autoResetEvent.Dispose();
            }
        }

        #endregion

        #region 连接

        /// <summary>
        /// Connect the radar, must call this function before operating the radar.
        /// </summary>
        /// <returns>In tcp mode, return true if connecting successfully. Always return true in udp mode</returns>
        /// <exception cref="NsrRadarSdk.NsrTypes.RadarException"></exception>
        /// <exception cref="NsrRadarSdk.NsrTypes.RadarSdkNotInitException">Thrown when use the sdk without init it.</exception>
        /// <exception cref="System.TimeoutException">Thrown when connection timed out.</exception>
        public bool Connect()
        {
            return NsrSdk.Instance.ConnectTo(this);
        }

        /// <summary>
        /// Disconnect the radar.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NsrRadarSdk.NsrTypes.RadarSdkNotInitException">Thrown when use the sdk without init it.</exception>
        public void DisConnect()
        {
            try
            {
                NsrSdk.Instance.DisConnectTo(this);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                SetOnlineState(false);
            }
        }

        #endregion

        #region 发送，组包


        /// <summary>
        /// Send message, and wait for response if waitResponse is true
        /// 发送数据，当waitResponse为ture时，等待雷达响应。
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <returns>Return true if waitResponse is false, or received response within time limit, otherwise return false.
        /// 如果在超时时间内收到雷达响应，则返回真</returns>
        /// <exception cref="System.TimeoutException">Thrown when response hasn't received within time limit.</exception>
        /// <exception cref="NsrRadarSdk.NsrTypes.RadarException">Thrown when get unknown errors.</exception>
        private bool Send(RVS_COMMAND cmd, byte[] buffer, int length = -1, bool waitResponse = true)
        {
            return Send(cmd, buffer, _port, length, waitResponse);
        }

        /// <summary>
        /// 向雷达指定端口发送数据并等待雷达响应。
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="buffer"></param>
        /// <param name="destPort"></param>
        /// <param name="length"></param>
        /// <param name="waitResponse"></param>
        /// <returns></returns>
        private bool Send(RVS_COMMAND cmd, byte[] buffer, int destPort, int length, bool waitResponse = true)
        {
            if (length < 0)
                length = buffer.Length;
            //释放和重置信号量
            _replyEventDictionary[cmd].Set();
            _replyEventDictionary[cmd].Reset();
            //接收包重置
            _replyPacketDictionary[cmd] = null;

            NsrSdk.Instance.Send(buffer, length, _ip.ToString(), destPort);

            int nWaitTime = NsrSdk.Instance.Timeout;
            if (!waitResponse)
                return true;

            bool ret = _replyEventDictionary[cmd].WaitOne(nWaitTime);
            if (ret == false)
            {
                //超时，抛出异常
                string exMsg = string.Format("No response after waitting for {0} milliseconds.", nWaitTime);
                throw new TimeoutException(exMsg);
            }

            //检查接收，判断命令执行是否成功
            rvs_FRAME_Context frame = null;
            if (!_replyPacketDictionary.TryGetValue(cmd, out frame))
            {
                throw new RadarException("The receive buffer may be destroyed");
            }

            if (frame != null)
            {
                //检测应答
                return frame.OperationSucceed;
            }
            else
            {
                //超时，抛出异常
                throw new RadarException("Select receive packet failed.");
            }

        }

        private byte[] MakePacket(RVS_COMMAND cmd, object cmd_params, out int bufLen)
        {
            return MakePacket(cmd, cmd_params, out bufLen, _devAddr);
        }

        /// <summary>
        /// 根据指定的命令和参数封装数据包
        /// </summary>
        /// <param name="cmd">待发送的命令</param>
        /// <param name="cmd_params">所有参数</param>
        /// <param name="bufLen">实际数据包大小</param>
        /// <returns>封装后的数据包</returns>
        private byte[] MakePacket(RVS_COMMAND cmd, object cmd_params, out int bufLen, RVS_DeviceAddress deviceAddress)
        {
            rvs_FRAME_Context context = new rvs_FRAME_Context();
            context.command = cmd;
            context.dstAddr = deviceAddress;
            context.srcAddr = RVS_DeviceAddress.PC_ADDR;
            int paramLen;
            if (cmd_params == null)
            {
                //没有参数部分
                paramLen = 0;
                context.paramLen = 0;
                context.paramObject = null;
            }
            else
            {
                paramLen = Marshal.SizeOf(cmd_params);
                context.paramObject = cmd_params;
                context.paramLen = (ushort)paramLen;
            }

            byte[] buffer = new byte[(RadarProtocol.NormalPacketLen + paramLen) * 2];
            int err = RadarProtocol.rvs_FormRVSPacket(buffer, buffer.Length, context);
            bufLen = (int)err;

            if (err < 0)
            {
                throw new RadarException("Make packet failed, result :" + ((RVS_ERROR)err).ToString());
            }
            return buffer;
        }

        #endregion

        #region 接收


        /// <summary>
        /// 解析数据包
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="buffer"></param>
        /// <returns>如果是报警目标上传，则返回true</returns>
        internal rvs_FRAME_Context ReceiveData(NsrSdk sender, byte[] buffer)
        {
            rvs_FRAME_Context context = new rvs_FRAME_Context();
            int packetLen = buffer.Length;
            RVS_ERROR err = RadarProtocol.rvs_UnpackCommand(ref context, buffer, ref packetLen);
            RVS_COMMAND result;
            Type _structType;

            if (RVS_ERROR.RVS_OK == err) //解析包失败则返回
            {
                _devAddr = context.srcAddr;
                _structType = null;
                result = context.command;
                byte[] cmd_params = (byte[])context.paramBytes;
                switch (context.command)
                {
                    case RVS_COMMAND.RVS_RESETCOMMAND:
                    case RVS_COMMAND.RVS_BUZZERCOMMAND:
                    case RVS_COMMAND.RVS_COORDINATECOMMAND:
                    case RVS_COMMAND.RVS_NETCOMMAND:
                    case RVS_COMMAND.RVS_HEARTBEATCOMMAND:
                    case RVS_COMMAND.RVS_ADDRESSCOMMAND:
                    case RVS_COMMAND.RVS_INSTALLPARAMCOMMAND:
                    case RVS_COMMAND.RVS_SAVEPARACOMMAND:
                    case RVS_COMMAND.RVS_SAVERAWDATACOMMAND:
                    case RVS_COMMAND.RVS_TranTESTCOMMAND:
                    case RVS_COMMAND.RVS_SAMPLEPARAMCOMMAND:
                    case RVS_COMMAND.RVS_ALGORITHMCOMMAND:
                    case RVS_COMMAND.RVS_SYSTEMTIMECOMMAND: //  所有普通命令的确认帧
                        _structType = typeof(RVS_PARAM_ANSWER);
                        break;
                    case RVS_COMMAND.RVS_STATUSREADCOMMAND: //  请求雷达状态，确认帧
                        _structType = typeof(rvs_PARAM_STATUS);
                        break;
                    case RVS_COMMAND.RVS_HEARTCOMMAND: //心跳包
                        _nHeartTime = cmd_params[0];
                        Interlocked.Exchange(ref _nMissHeartCount, 0);

                        ReplyHeart();
                        break;
                    case RVS_COMMAND.RVS_TARGETSENDCOMMAND_A8: //目标信息上传 其他雷达
                        RVS_Target_List targetList = new RVS_Target_List(cmd_params, "A8");
                        context.paramObject = targetList;
                        break;
                    case RVS_COMMAND.RVS_TARGETSENDCOMMAND_A9: //目标信息上传 800W使用
                        RVS_Target_List targetList800 = new RVS_Target_List(cmd_params, "A9");
                        context.paramObject = targetList800;
                        break;
                    case RVS_COMMAND.RVS_BROADCASTCOMMAND: //雷达广播自己的信息
                        break;
                    default:
                        result = RVS_COMMAND.NULL;
                        break;
                }

                if (_structType != null)
                {
                    context.paramObject = RadarProtocol.BytesToStructure(cmd_params, _structType);
                    _replyPacketDictionary[context.command] = context;
                    _replyEventDictionary[context.command].Set();
                }
            }
            else
            {
                context.command = RVS_COMMAND.NULL;
            }

            return context;
        }

        /// <summary>
        /// 解析雷达广播包
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        internal static rvs_FRAME_Context ReceiveStrangeData(byte[] buffer)
        {
            rvs_FRAME_Context context = new rvs_FRAME_Context();
            int packetLen = buffer.Length;
            if (RVS_ERROR.RVS_OK != RadarProtocol.rvs_UnpackCommand(ref context, buffer, ref packetLen))
                return context;

            switch (context.command)
            {
                case RVS_COMMAND.RVS_BROADCASTCOMMAND: //雷达向外广播本机信息
                    byte[] paramBytes = context.paramBytes;
                    if (paramBytes.Length < Marshal.SizeOf(typeof(RVS_PARAM_BROADCAST)))
                    {
                        byte[] tmpBytes = new byte[Marshal.SizeOf(typeof(RVS_PARAM_BROADCAST))];
                        paramBytes.CopyTo(tmpBytes, 0);
                        paramBytes = tmpBytes;
                    }
                    RVS_PARAM_BROADCAST broadcastInfo =
                            (RVS_PARAM_BROADCAST)
                            RadarProtocol.BytesToStructure(paramBytes, typeof(RVS_PARAM_BROADCAST));
                    context.paramObject = broadcastInfo;

                    break;
                case RVS_COMMAND.RVS_HEARTCOMMAND:
                    break;
                default:
                    break;
            }
            return context;
        }


        #endregion

        #region 雷达功能

        #region 设置参数

        /// <summary>
        /// Enable or disable the buzzer
        /// </summary>
        /// <param name="enable">Set true to enable the buzzer, or false to disable it</param>
        /// <returns>Return true if succeeded</returns>
        /// <exception cref="System.TimeoutException">Thrown when request timed out.</exception>
        /// <exception cref="NsrRadarSdk.NsrTypes.RadarException">Thrown when get unknown errors.</exception>
        public bool SetBuzzer(bool enable)
        {
            RVS_PARAM_BUZZERSET cmd_params = new RVS_PARAM_BUZZERSET();
            cmd_params.IsOpen = enable;
            int len;

            RVS_COMMAND cmd = RVS_COMMAND.RVS_BUZZERCOMMAND;
            byte[] buffer = MakePacket(cmd, cmd_params, out len);
            bool ret = Send(cmd, buffer, len);

            return ret;
        }

        /// <summary>
        /// Set the coordinate filter.
        /// </summary>
        /// <param name="point">The filter coordinates, must has 4 points</param>
        /// <returns>Return true if succeeded</returns>
        /// <exception cref="System.TimeoutException">Thrown when request timed out.</exception>
        /// <exception cref="NsrRadarSdk.NsrTypes.RadarException">Thrown when get unknown errors.</exception>
        /// <exception cref="System.ArgumentException"></exception>
        public bool SetCoordinate(PointF[] point)
        {
            if (null == point || point.Length < 4)
                throw new ArgumentException("point num less than 4", "point");

            for (int i = 0; i < 4; i++)
            {
                int retry;
                for (retry = 0; retry < 3; retry++)
                {
                    if (SetCoordinate((byte)(i + 1), point[i]))
                        break;
                }
                if (retry >= 3)
                {
                    string msg = string.Format("Failed to set radar coordinates{0}", i.ToString());
                    throw new RadarException(msg);
                }
            }

            SaveParam();

            return true;
        }

        /// <summary>
        /// 设定指定id号的过滤坐标
        /// </summary>
        /// <param name="id"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private bool SetCoordinate(byte id, PointF point)
        {
            RVS_PARAM_COORDINATE coordinate = new RVS_PARAM_COORDINATE();
            coordinate.coordinateNo = id;
            coordinate.X = point.X;
            coordinate.Y = point.Y;

            int len;
            RVS_COMMAND cmd = RVS_COMMAND.RVS_COORDINATECOMMAND;
            byte[] buffer = MakePacket(cmd, coordinate, out len);
            return Send(cmd, buffer, len);
        }


        /// <summary>
        /// Set heart time.
        /// </summary>
        /// <param name="seconds">Heart time value to be set</param>
        /// <returns>Return true if succeeded</returns>
        /// <exception cref="System.TimeoutException">Thrown when request timed out.</exception>
        /// <exception cref="NsrRadarSdk.NsrTypes.RadarException">Thrown when get unknown errors.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public bool SetHeartTime(byte seconds)
        {
            if (seconds == 0)
                throw new ArgumentOutOfRangeException("seconds", "Heartbeat time cannot be zero.");

            RVS_PARAM_HEART cmd_params;
            cmd_params.time = seconds;
            int len;

            RVS_COMMAND cmd = RVS_COMMAND.RVS_HEARTBEATCOMMAND;
            byte[] buffer = MakePacket(cmd, cmd_params, out len);
            bool ret = Send(cmd, buffer, len);

            return ret;
        }

        /// <summary>
        /// Change radar's network parameters.
        /// </summary>
        /// <param name="newIp">New ip address</param>
        /// <param name="netmask">Net mask</param>
        /// <param name="gateway">Gateway</param>
        /// <returns>Return true if succeeded</returns>
        /// <exception cref="System.TimeoutException">Thrown when request timed out.</exception>
        /// <exception cref="NsrRadarSdk.NsrTypes.RadarException">Thrown when get unknown errors.</exception>
        public bool SetIpAddress(IPAddress newIp, IPAddress netmask, IPAddress gateway)
        {
            RVS_PARAM_NET cmd_params = new RVS_PARAM_NET(newIp, netmask, gateway);
            int len;
            RVS_COMMAND cmd = RVS_COMMAND.RVS_NETCOMMAND;

            byte[] buffer = MakePacket(cmd, cmd_params, out len);
            return Send(cmd, buffer, len, false);
        }

        public bool SetTime(DateTime dt)
        {


            RVS_SYSTEMTIMECOMMAND cmd_params;
            cmd_params.Y = (byte)(dt.Year - 1970);
            cmd_params.M = (byte)dt.Month;
            cmd_params.D = (byte)dt.Day;
            cmd_params.H = (byte)dt.Hour;
            cmd_params.m = (byte)dt.Minute;
            cmd_params.S = (byte)dt.Second;

            int len;

            RVS_COMMAND cmd = RVS_COMMAND.RVS_SYSTEMTIMECOMMAND;
            byte[] buffer = MakePacket(cmd, cmd_params, out len);
            bool ret = Send(cmd, buffer, len);

            return ret;
        }

        /// <summary>
        /// Save param.
        /// </summary>
        /// <returns></returns>
        private bool SaveParam()
        {
            int len;

            RVS_COMMAND cmd = RVS_COMMAND.RVS_SAVEPARACOMMAND;
            byte[] buffer = MakePacket(cmd, null, out len);
            bool ret = Send(cmd, buffer, len);

            return ret;
        }

        #endregion

        #region 获取参数

        /// <summary>
        /// Get buzzer state.
        /// </summary>
        /// <returns>Return true if buzzer is enabled</returns>
        /// <exception cref="System.TimeoutException">Thrown when request timed out.</exception>
        /// <exception cref="NsrRadarSdk.NsrTypes.RadarException">Thrown when get unknown errors.</exception>
        public bool GetBuzzer()
        {
            rvs_PARAM_STATUS frame = new rvs_PARAM_STATUS();
            bool result = GetStatus(ref frame);
            if (result)
            {
                return frame.bee.IsOpen;
            }
            else
            {
                throw new RadarException("unknown error");
            }
        }

        /// <summary>
        /// Get the filter coordinates.
        /// </summary>
        /// <returns>The filter coordinates</returns>
        /// <exception cref="System.TimeoutException">Thrown when request timed out.</exception>
        /// <exception cref="NsrRadarSdk.NsrTypes.RadarException">Thrown when get unknown errors.</exception>
        public PointF[] GetFilterCoordinate()
        {
            rvs_PARAM_STATUS frame = new rvs_PARAM_STATUS();
            bool result = GetStatus(ref frame);
            if (result)
            {
                return _ptsAlarmAreaVertices;
            }
            else
            {
                throw new RadarException("unknown error");
            }
        }

        /// <summary>
        /// Get heart time.
        /// </summary>
        /// <returns>Heart time</returns>
        /// <exception cref="System.TimeoutException">Thrown when request timed out.</exception>
        /// <exception cref="NsrRadarSdk.NsrTypes.RadarException">Thrown when get unknown errors.</exception>
        public byte GetHeartTime()
        {
            rvs_PARAM_STATUS frame = new rvs_PARAM_STATUS();
            bool result = GetStatus(ref frame);
            if (result)
            {
                return frame.heart.time;
            }
            else
            {
                throw new RadarException("unknown error");
            }
        }

        /// <summary>
        /// Get status of the radar.
        /// </summary>
        /// <param name="frame">The rvs_PARAM_STATUS struct that contains radar's status</param>
        /// <returns>Return true is succeeded</returns>
        /// <exception cref="System.TimeoutException">Thrown when request timed out.</exception>
        /// <exception cref="NsrRadarSdk.NsrTypes.RadarException">Thrown when get unknown errors.</exception>
        public bool GetStatus(ref rvs_PARAM_STATUS frame)
        {
            int len;
            RVS_COMMAND cmd = RVS_COMMAND.RVS_STATUSREADCOMMAND;
            byte[] buffer = MakePacket(cmd, null, out len, RVS_DeviceAddress.BroadCast);

            bool result = false;
            result = Send(cmd, buffer, len);
            if (result)
            {
                rvs_FRAME_Context frameContext;
                if (_replyPacketDictionary.TryGetValue(cmd, out frameContext))
                {
                    frame = (rvs_PARAM_STATUS)frameContext.paramObject;

                    PointF[] pts = new PointF[frame.position.Length];
                    int flag = 0;
                    for (int i = 0; i < pts.Length; i++)
                    {
                        float x = frame.position[i].coordinate_X.Value;
                        float y = frame.position[i].coordinate_Y.Value;
                        int index = frame.position[i].coordinateNo - 1;
                        if (index >= 0 && index < pts.Length)
                        {
                            pts[index] = new PointF(x, y);
                            flag |= (1 << index);
                        }
                    }

                    if (pts.Length == 4 && flag == 0x0F)
                    {
                        _ptsAlarmAreaVertices = pts;
                    }
                    else
                    {
                        throw new RadarException("Radar coordinate filter read error.");
                    }

                    _nHeartTime = frame.heart.time;
                    _uAlgorithmVersion = frame.radarVerInfo.radarAlgorithm;
                    _uFirmwareVersion = frame.radarVerInfo.armSoftVersion;
                    _bBuzzer = frame.bee.IsOpen;
                }
                else
                {
                    throw new RadarException("The receive buffer may be destroyed");
                }
            }

            return result;
        }


        #endregion

        #region 心跳，在线状态

        private void ReplyHeart()
        {
            RVS_PARAM_HEART_ANSWER cmd_params = RVS_PARAM_HEART_ANSWER.GetDefault();
            int len;

            RVS_COMMAND cmd = RVS_COMMAND.RVS_ANSWERCOMMAND;
            byte[] buffer = MakePacket(cmd, cmd_params, out len);
            bool ret = Send(cmd, buffer, len, false);
        }

        /// <summary>
        /// 设置雷达在线状态，且不引发事件
        /// </summary>
        /// <param name="online"></param>
        internal void SetOnlineState(bool online)
        {
            _bOnline = online;
        }

        #endregion

        #endregion
    }
}
