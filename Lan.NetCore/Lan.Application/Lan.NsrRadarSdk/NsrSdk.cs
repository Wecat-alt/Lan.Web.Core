using CAT.NsrRadarSdk.NsrTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CAT.NsrRadarSdk
{
    /// <summary>
    /// Nsr sdk class, include global sdk function.
    /// </summary>
    public class NsrSdk
    {
        #region 事件和委托

        /// <summary>
        /// The function called when radar's online/offline state changed.
        /// </summary>
        /// <param name="radar">The radar that state has changed</param>
        /// <param name="online">Indicate whether the radar is going online</param>
        public delegate void RadarConnectionStateDelegate(NsrRadar radar, bool online);

        /// <summary>
        /// The function called when radar detect any target.
        /// </summary>
        /// <param name="radar">The radar the detected any target</param>
        /// <param name="targetList">Target list</param>
        public delegate void TargetDetectDelegate(NsrRadar radar, RVS_Target_List targetList);

        /// <summary>
        /// The function called when the sdk received radar's broadcast message.
        /// </summary>
        /// <param name="radar">The radar that has been searched</param>
        /// <param name="info">Radar broadcast info</param>
        public delegate void RadarBroadcastDelegate(NsrRadar radar, ref RVS_PARAM_BROADCAST info);

        /// <summary>
        /// Happens when radar's online/offline state changed.
        /// 当雷达上线/下线时发生
        /// </summary>
        public event RadarConnectionStateDelegate RadarOnlineStateChanged = null;

        /// <summary>
        /// Happens when radar detect any target.
        /// 当雷达探测到目标时发生
        /// </summary>
        public event TargetDetectDelegate TargetDetect = null;

        /// <summary>
        /// Happens when the sdk received radar's broadcast message.
        /// 当接收到雷达广播信息时发生
        /// </summary>
        public event RadarBroadcastDelegate RadarBroadcast = null;

        internal void OnRadarOnlineStateChanged(NsrRadar radar, bool online)
        {
            if (_useTcp && online == false)
            {
                radar.DisConnect();
            }
            if (RadarOnlineStateChanged != null)
                RadarOnlineStateChanged(radar, online);
        }

        /// <summary>
        /// 引发雷达目标上报事件
        /// </summary>
        /// <param name="radar"></param>
        internal void OnTargetDetect(NsrRadar radar, RVS_Target_List targetList)
        {
            if (TargetDetect != null)
            {
                Task.Factory.StartNew(() => TargetDetect(radar, targetList));
            }
        }

        internal void OnRadarBroadcast(NsrRadar radar, ref RVS_PARAM_BROADCAST info)
        {
            if (RadarBroadcast != null)
                RadarBroadcast(radar, ref info);
        }

        #endregion

        #region 单例

        private static NsrSdk instance = null;
        private static object syncRoot = new object();

        /// <summary>
        /// Get the unique instance of class NsrSdk.
        /// </summary>
        public static NsrSdk Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new NsrSdk();
                        }
                    }
                }

                return instance;
            }
        }

        #endregion

        #region 字段

        private readonly ConcurrentDictionary<string, NsrRadar> _dictRadars;    //已连接的雷达列表
        private Thread _receiveThread;          //目标接收线程，只有udp模式启用
        private Thread _broadcastThread;        //广播接收线程
        private bool _isDisposing;              //是否需要释放sdk资源
        private bool _isThreadContinue;         //是否允许线程继续运行
        private bool _enableBroadcast;          //是否启用广播
        private IRadarSocket _radarSocket;      //连接管理类
        private ConcurrentQueue<RadarMessage> _sendMessageQueue;    //消息发送队列
        private bool _useTcp;                           //是否使用tcp模式
        private bool _isInitialized = false;            //是否已经初始化sdk
        private System.Timers.Timer _timerHeartBeat;    //处理心跳超时的定时器

        #endregion

        #region 属性


        /// <summary>
        /// Get or set the timeout(ms) for receiving message from radar.
        /// 获取或设置接收雷达回复的超时时间，单位毫秒。
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Get the local port used by sdk.
        /// 获取sdk当前使用的本地端口。
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Indicate the radar will go offline after losing how many heart messages.
        /// 表示当丢失多少次心跳包后，认为雷达掉线。
        /// </summary>
        public int MissHeartTimeForOffline { get; set; }

        /// <summary>
        /// Get the working network protocol, Tcp or Udp.
        /// 获取当前使用的网络协议。
        /// </summary>
        public string NetworkType
        {
            get
            {
                CheckSdkInit();
                if (_useTcp)
                {
                    return "Tcp";
                }
                else
                {
                    return "Udp";
                }
            }
        }

        #endregion

        #region 构造

        private NsrSdk()
        {
            Timeout = 3000;
            MissHeartTimeForOffline = 3;
            _enableBroadcast = false;
            _isDisposing = false;
            _dictRadars = new ConcurrentDictionary<string, NsrRadar>();
            _sendMessageQueue = new ConcurrentQueue<RadarMessage>();
            _receiveThread = null;
            _broadcastThread = null;
            _timerHeartBeat = null;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// Initialize the sdk.
        /// 初始化sdk。
        /// </summary>
        /// <param name="localPort">local port to bind, this param is ignored if useTcp is true.</param>
        /// <param name="useTcp">indicate whether to use tcp or udp</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when localPort is invalid.</exception>
        /// <exception cref="System.Net.Sockets.SocketException">Thrown when bind port failed.</exception>
        /// <returns></returns>
        public void Init(int localPort, bool useTcp)
        {
            if (_isInitialized)
                return;

            _radarSocket = null;
            _receiveThread = null;
            _timerHeartBeat = null;

            try
            {
                IRadarSocket radarSocket = null;
                if (useTcp)
                {
                    radarSocket = new RadarTcp();
                }
                else
                {
                    radarSocket = new RadarUdp();
                    radarSocket.Bind(localPort);
                }

                Port = localPort;
                _useTcp = useTcp;
                _radarSocket = radarSocket;

                _receiveThread = new Thread(new ParameterizedThreadStart(DataReceiveThread));
                _receiveThread.Name = "radar data receive";
                _receiveThread.IsBackground = true;

                _timerHeartBeat = new System.Timers.Timer();
                _timerHeartBeat.Elapsed += new ElapsedEventHandler(HeartBeatEvent);
                _timerHeartBeat.AutoReset = false;
                _timerHeartBeat.Interval = 5000;

                _isThreadContinue = true;
                if (!useTcp)
                {
                    //Udp模式开启接收线程，Tcp模式使用异步方式，不开线程
                    _receiveThread.Start(radarSocket);
                    //Udp模式下启用心跳判断在线状态
                    _timerHeartBeat.Start();
                }
                _isDisposing = false;
                _isInitialized = true;
            }
            catch (Exception)
            {
                _isThreadContinue = false;

                if (_timerHeartBeat != null)
                {
                    _timerHeartBeat.Stop();
                    _timerHeartBeat.Close();
                    _timerHeartBeat = null;
                }

                if (_receiveThread != null)
                {
                    _receiveThread.Join(2000);
                    if (_receiveThread.IsAlive)
                        _receiveThread.Abort();
                    _receiveThread = null;
                }

                _dictRadars.Clear();
                if (_sendMessageQueue.Count != 0)
                {
                    _sendMessageQueue = new ConcurrentQueue<RadarMessage>();
                }

                if (_radarSocket != null)
                {
                    _radarSocket.Dispose();
                    _radarSocket = null;
                }

                throw;
            }

        }

        /// <summary>
        /// Clean up all resource the sdk is using.
        /// 清理sdk资源
        /// </summary>
        /// <exception cref="System.Net.Sockets.SocketException"></exception>
        public void CleanUp()
        {
            if (_isDisposing || _isInitialized == false)
                return;
            _isThreadContinue = false;
            _enableBroadcast = false;
            _isDisposing = true;
            WaitForThreadExit();

            _radarSocket.Close();
            RadarBroadcast = null;
            _isInitialized = false;
        }

        /// <summary>
        /// Start receive radar broadcast messgae, and call the callback function when receive a broadcast messgae.
        /// 启用雷达广播接收
        /// </summary>
        /// <param name="callback">The callback function</param>
        /// <exception cref="NsrRadarSdk.NsrTypes.RadarException"></exception>
        public void StartReceiveBroadcast(RadarBroadcastDelegate callback)
        {
            CheckSdkInit();
            const int radarBroadcastPort = 7773;

            RadarUdp broadrcastSocket = new RadarUdp();
            try
            {
                broadrcastSocket.Bind(radarBroadcastPort);
            }
            catch (SocketException socketException)
            {
                throw new RadarException(string.Format("Bind broadcast port {0} failed.", radarBroadcastPort),
                    socketException);
            }
            catch (Exception)
            {
                throw;
            }

            try
            {
                _broadcastThread = new Thread(new ParameterizedThreadStart(BroadcastReceiveThread));
                _broadcastThread.Name = "radar broadcast receive";
                _broadcastThread.IsBackground = true;
                _enableBroadcast = true;
                _broadcastThread.Start(broadrcastSocket);
            }
            catch (Exception)
            {
                broadrcastSocket.Dispose();
                _broadcastThread = null;
                throw;
            }

            RadarBroadcast = callback;
        }

        /// <summary>
        /// Stop receive radar broadcast messgae.
        /// 停止接收雷达广播
        /// </summary>
        public void StopReceiveBroadcast()
        {
            CheckSdkInit();
            _enableBroadcast = false;
            RadarBroadcast = null;
            var thread = _broadcastThread;
            if (thread != null && thread.IsAlive)
            {
                thread.Abort();
                _broadcastThread = null;
            }
        }


        ///<summary>
        /// Instantiate a NsrRadar object by radar ip and port.
        /// 使用指定的ip和端口号实例化一个雷达对象。
        /// </summary>
        /// <param name="ip">Radar ip address</param>
        /// <param name="port">Radar port</param>
        /// <exception cref="NsrRadarSdk.NsrTypes.RadarException">Thrown when the sdk hasn't been initialized.</exception>
        /// <returns>The radar class that created</returns>
        public NsrRadar CreateRadar(string ip, int port)
        {
            CheckSdkInit();
            NsrRadar radar;
            if (_dictRadars.TryGetValue(ip, out radar))
            {
                //如果已存在，直接返回
                return radar;
            }

            radar = new NsrRadar();
            radar.Ip = ip;
            radar.Port = port;
            radar.DevAddress = RVS_DeviceAddress.BroadCast;

            if (_dictRadars.ContainsKey(radar.Ip) == false)
            {
                _dictRadars[radar.Ip] = radar;
            }
            return radar;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 检查sdk是否初始化
        /// </summary>
        private void CheckSdkInit()
        {
            if (false == _isInitialized)
            {
                throw new RadarSdkNotInitException("The sdk has not been initialized.");
            }
        }

        /// <summary>
        /// 等待接收线程和广播线程停止
        /// </summary>
        private void WaitForThreadExit()
        {
            var threads = new Thread[] { _receiveThread, _broadcastThread };
            Parallel.ForEach(threads, (thread, state) =>
            {
                if (thread != null && thread.IsAlive)
                {
                    thread.Join(2000);
                    if (thread.IsAlive)
                        thread.Abort();
                }
            });
        }

        /// <summary>
        /// 心跳超时计数，每一秒对所有在线的雷达计数加1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HeartBeatEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                NsrRadar[] radars = _dictRadars.Values.ToArray();
                foreach (NsrRadar radar in radars)
                {
                    if (radar.Online)
                    {
                        radar.MissHeartCount++;
                    }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                _timerHeartBeat.Interval = 1000;
                if (_isThreadContinue)
                    _timerHeartBeat.Start();
            }
        }

        /// <summary>
        /// 连接指定雷达
        /// </summary>
        /// <param name="radar"></param>
        /// <returns></returns>
        internal bool ConnectTo(NsrRadar radar)
        {
            CheckSdkInit();

            if (_useTcp)
            {
                bool isConnected = _radarSocket.Connect(radar);
                if (isConnected == false)
                {
                    return false;
                }
            }

            if (_dictRadars.ContainsKey(radar.Ip) == false)
            {
                _dictRadars[radar.Ip] = radar;
            }
            radar.GetHeartTime();

            return true;
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="radar"></param>
        /// <returns></returns>
        internal void DisConnectTo(NsrRadar radar)
        {
            CheckSdkInit();
            NsrRadar tmpRadar;
            _dictRadars.TryRemove(radar.Ip, out tmpRadar);
            _radarSocket.Close(radar.Ip);
        }

        /// <summary>
        /// 将指定的数据添加到发送队列
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <param name="destIp"></param>
        /// <param name="destPort"></param>
        internal void Send(byte[] buffer, int length, string destIp, int destPort)
        {
            RadarMessage data = new RadarMessage();
            data.Buffer = buffer;
            data.Length = length;
            data.Ip = destIp;
            data.Port = destPort;
            if (_useTcp)
            {
                _radarSocket.Send(data);
            }
            else
            {
                _sendMessageQueue.Enqueue(data);
            }
        }

        /// <summary>
        /// 广播接收线程
        /// </summary>
        /// <param name="param"></param>
        private void BroadcastReceiveThread(object param)
        {
            RadarUdp radarSocket = param as RadarUdp;
            if (radarSocket == null)
            {
                throw new RadarException("Radar socket init error.");
            }

            while (_isThreadContinue)
            {
                try
                {
                    Thread.Sleep(1);
                    RadarMessage recvMessage = radarSocket.ReceiveOnePacket();

                    if (null == recvMessage)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    NsrRadar radar;
                    rvs_FRAME_Context context = NsrRadar.ReceiveStrangeData(recvMessage.Buffer);
                    bool bContains = _dictRadars.TryGetValue(recvMessage.Ip, out radar);

                    if (!bContains || radar == null)
                    {
                        //列表中不存在这个雷达，new一个
                        radar = new NsrRadar();
                        radar.Ip = recvMessage.Ip;
                        radar.SetOnlineState(false);
                        radar.Port = _useTcp ? 50000 : 8100;
                    }

                    radar.DevAddress = context.srcAddr;
                    if (context.paramBytes != null)
                    {
                        radar.DevAddressNew = (RVS_DeviceAddressNEW)context.paramBytes[9];

                        //RVS_PARAM_BROADCAST broadcast = (RVS_PARAM_BROADCAST)context.paramObject;

                        //radar.MacAddress = broadcast.MacAddress;
                        //radar.FirmwareVersion = broadcast.FirmwareVersion;

                        //Task.Factory.StartNew((() =>
                        //{
                        //    NsrSdk.Instance.OnRadarBroadcast(radar, ref broadcast);
                        //})).ContinueWith(t => { },
                        //    TaskContinuationOptions.OnlyOnFaulted);
                    }
                }
                catch (ObjectDisposedException)
                {
                    if (!_isThreadContinue)
                        break;
                }
                catch (SocketException)
                {
                    if (!_isThreadContinue)
                        break;
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {

                }
            }

            radarSocket.Close();
        }

        /// <summary>
        /// 雷达数据接收线程
        /// </summary>
        private void DataReceiveThread(object param)
        {
            IRadarSocket radarSocket = param as IRadarSocket;
            if (radarSocket == null)
            {
                throw new RadarException("Radar socket init error.");
            }

            while (_isThreadContinue)
            {
                try
                {
                    if (_sendMessageQueue.Count != 0)
                    {
                        RadarMessage data;
                        if (_sendMessageQueue.TryDequeue(out data))
                        {
                            radarSocket.Send(data);
                        }
                    }

                    RadarMessage recvMessage = radarSocket.ReceiveOnePacket();

                    if (null == recvMessage)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    NsrRadar radar;
                    bool bContains = _dictRadars.TryGetValue(recvMessage.Ip, out radar);

                    if (!bContains)
                    {
                        continue;
                    }

                    rvs_FRAME_Context context = radar.ReceiveData(this, recvMessage.Buffer);
                    if (context.command != RVS_COMMAND.NULL)
                    {
                        radar.DevAddress = context.srcAddr;
                        radar.DevAddressNew = (RVS_DeviceAddressNEW)context.paramBytes[9];
                        radar.Online = true;
                    }
                    if (context.command == RVS_COMMAND.RVS_TARGETSENDCOMMAND_A8 || context.command == RVS_COMMAND.RVS_TARGETSENDCOMMAND_A9) //解析包，如果是报警目标上传信息，则引发事件
                    {
                        OnTargetDetect(radar, context.paramObject as RVS_Target_List);
                    }
                }
                catch (ObjectDisposedException)
                {
                    if (!_isThreadContinue)
                        break;
                }
                catch (SocketException)
                {
                    if (!_isThreadContinue)
                        break;
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {

                }
            }

            radarSocket.Close();

            var timer = _timerHeartBeat;
            if (timer != null)
            {
                try
                {
                    timer.Close();
                }
                catch (Exception ex)
                {

                }
                _timerHeartBeat = null;
            }
        }

        #endregion
    }
}
