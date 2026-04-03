using System.Net;
using System.Net.Sockets;

namespace CAT.NsrRadarSdk.SocketManage
{
    /// <summary>
    /// 异步TCP客户端
    /// </summary>
    internal class SocketClient : IDisposable
    {
        enum ClientState
        {
            Init,               //初始化
            Connecting,         //正在连接
            IsConnected,        //已连接
            LostConnection,     //连接丢失（被动）
            DisConnected,       //断开连接（主动）
        }

        #region 字段
        /// <summary>
        /// 客户端连接Socket
        /// </summary>
        private Socket clientSocket;
        /// <summary>
        /// 当前连接状态
        /// </summary>
        private ClientState state;
        //        /// <summary>
        //        /// 连接状态
        //        /// </summary>
        //        private Boolean connected = false;
        /// <summary>
        /// 连接点
        /// </summary>
        private IPEndPoint hostEndPoint;
        /// <summary>
        /// 处理连接与接收的SocketAsyncEventArgs
        /// </summary>
        private SocketAsyncEventArgs listenerSocketAsyncEventArgs;
        /// <summary>
        /// 连接信号量
        /// </summary>
        private AutoResetEvent autoConnectEvent = new AutoResetEvent(false);
        /// <summary>
        /// 用于发送的套接字状态列表
        /// </summary>
        private List<MySocketAsyncEventArgs> mysockList = new List<MySocketAsyncEventArgs>();

        private NsrRadar radar;

        #endregion

        #region 委托事件
        /// <summary>
        /// 开始监听数据的事件
        /// </summary>
        public event Action<SocketClient> OnClientStarted;
        /// <summary>
        /// 远程服务关闭事件
        /// </summary>
        public event Action<SocketClient> OnServerStoped;
        /// <summary>
        /// 接收到数据时调用的事件
        /// </summary>
        public event Action<SocketClient, byte[]> OnDataReceived;
        /// <summary>
        /// 发送信息完成的事件
        /// </summary>
        public event Action<SocketClient, bool> OnSended;

        #endregion

        #region 属性

        /// <summary>
        /// 客户端是否与服务端建立连接
        /// </summary>
        public bool IsConnected { get { return state == ClientState.IsConnected; } }

        /// <summary>
        /// 远程连接端点
        /// </summary>
        public IPEndPoint RemoteEndPoint { get { return hostEndPoint; } }

        public byte[] ReceiveBuffer { get; set; }

        public int ReceiveBytes { get; set; }

        public NsrRadar Radar
        {
            get { return radar; }
        }

        #endregion

        #region 构造
        /// <summary>
        /// 初始化客户端
        /// </summary>
        /// <param name="hostName">服务端地址{IP地址}</param>
        /// <param name="port">端口号</param>
        public SocketClient(NsrRadar radar)
        {
            this.hostEndPoint = radar.RadarEndPoint;
            this.clientSocket = new Socket(this.hostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            this.radar = radar;
            state = ClientState.Init;
            ReceiveBuffer = new byte[0];
            ReceiveBytes = 0;
            //只给接收的SocketAsyncEventArgs设置缓冲区
            //this.bufferManager.SetBuffer(readWriteEventArgWithId.ReceiveAsyncEventArgs);
        }



        #endregion

        #region 私有方法
        /// <summary>  
        /// 初始化发送参数MySocketEventArgs  
        /// </summary>  
        /// <returns></returns>  
        private MySocketAsyncEventArgs InitMySocketAsyncEventArgs()
        {
            MySocketAsyncEventArgs sendArg = new MySocketAsyncEventArgs("Send", false);
            sendArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);
            sendArg.UserToken = clientSocket;
            sendArg.RemoteEndPoint = hostEndPoint;

            lock (mysockList)
            {
                mysockList.Add(sendArg);
            }
            return sendArg;
        }

        /// <summary>
        /// 连接的完成方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnConnect(object sender, SocketAsyncEventArgs e)
        {
            bool connected = (e.SocketError == SocketError.Success);
            state = connected ? ClientState.IsConnected : ClientState.Init;

            autoConnectEvent.Set();
            if (connected)
            {
                //触发服务启动事件
                if (OnClientStarted != null) OnClientStarted(this);
                listenerSocketAsyncEventArgs = new SocketAsyncEventArgs();
                byte[] receiveBuffer = new byte[32768];
                listenerSocketAsyncEventArgs.UserToken = clientSocket;
                listenerSocketAsyncEventArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
                listenerSocketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceive);

                if (!e.ConnectSocket.ReceiveAsync(listenerSocketAsyncEventArgs))
                    ProcessReceive(listenerSocketAsyncEventArgs);
            }
        }

        /// <summary>
        /// 接收的完成方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReceive(object sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }

        /// <summary>
        /// 发送的完成方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSend(object sender, SocketAsyncEventArgs e)
        {
            MySocketAsyncEventArgs mysock = (MySocketAsyncEventArgs)e;
            mysock.IsUsing = false;  //发送完成，将使用状态置否

            if (e.SocketError == SocketError.Success)
            {
                if (OnSended != null)
                    OnSended(this, true);
            }
            else
            {
                if (OnSended != null)
                    OnSended(this, false);
                this.ProcessError(e);
            }
        }

        /// <summary>
        /// 处理数据接收
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success && e.BytesTransferred != 0)
                {
                    byte[] dataBytes = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, dataBytes, 0, dataBytes.Length);

                    if (OnDataReceived != null)
                        OnDataReceived(this, dataBytes);

                    //继续投递Socket Receive事件
                    if (!(e.UserToken as Socket).ReceiveAsync(e))
                        ProcessReceive(e);
                }
                else
                {
                    ProcessError(e);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 处理错误
        /// </summary>
        /// <param name="e"></param>
        private void ProcessError(SocketAsyncEventArgs e)
        {
            Socket s = e.UserToken as Socket;

            if (s.Connected)
            {
                try
                {
                    s.Close();
                }
                catch (Exception)
                {
                    //client already closed
                }
            }

            state = ClientState.LostConnection;
            //移除事件订阅
            foreach (MySocketAsyncEventArgs mysock in mysockList)
                mysock.Completed -= OnSend;
            listenerSocketAsyncEventArgs.Completed -= OnReceive;

            //触发服务停止事件
            if (OnServerStoped != null)
                OnServerStoped(this);
        }

        #endregion

        #region 公共方法
        /// <summary>
        /// 连接服务端
        /// </summary>
        public bool Connect()
        {
            switch (state)
            {
                case ClientState.Init:
                    break;
                case ClientState.Connecting:
                    break;
                case ClientState.IsConnected:
                    throw new SocketException((int)SocketError.IsConnected);
                    break;
                case ClientState.LostConnection:
                case ClientState.DisConnected:
                    throw new SocketException((int)SocketError.Shutdown);
                    break;
                default:
                    break;
            }

            state = ClientState.Connecting;
            SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
            connectArgs.UserToken = this.clientSocket;
            connectArgs.RemoteEndPoint = this.hostEndPoint;
            connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnect);

            if (!clientSocket.ConnectAsync(connectArgs))
            {
                OnConnect(null, connectArgs);
            }
            autoConnectEvent.WaitOne(); //等待连接结果
            return IsConnected;
        }

        /// <summary>
        /// 发送字节数组
        /// </summary>
        /// <param name="dataBytes"></param>
        public void Send(byte[] dataBytes, int length = -1)
        {
            if (IsConnected)
            {
                //查找是否存在空闲MySocketAsyncEventArgs,存在即取出使用，不存在则创建
                MySocketAsyncEventArgs sendArgs = mysockList.Find(a => a.IsUsing == false);
                if (sendArgs == null)
                {
                    sendArgs = InitMySocketAsyncEventArgs();
                }
                lock (sendArgs) //锁定，避免抢占资源.  
                {
                    sendArgs.IsUsing = true;
                    if (length == -1)
                    {
                        length = dataBytes.Length;
                    }
                    sendArgs.SetBuffer(dataBytes, 0, length);
                }
                clientSocket.SendAsync(sendArgs);
            }
            else
            {
                throw new SocketException((Int32)SocketError.NotConnected);
            }

            /*
            if (dataBytes != null && dataBytes.Length > 0)
            {
                if (this.connected)
                {
                    SocketAsyncEventArgs senderSocketAsyncEventArgs = new SocketAsyncEventArgs();
                    senderSocketAsyncEventArgs.UserToken = this.clientSocket;
                    senderSocketAsyncEventArgs.SetBuffer(dataBytes, 0, dataBytes.Length);
                    senderSocketAsyncEventArgs.RemoteEndPoint = this.hostEndPoint;
                    senderSocketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);
                    clientSocket.SendAsync(senderSocketAsyncEventArgs);
                }
                else
                {
                    throw new SocketException((Int32)SocketError.NotConnected);
                }
            }*/
        }

        /// <summary>
        /// 添加数据到接收缓冲区末尾
        /// </summary>
        /// <param name="newRecvBytes"></param>
        public void AppendData(byte[] newRecvBytes)
        {
            if (ReceiveBuffer.Length - ReceiveBytes < newRecvBytes.Length)
            {
                byte[] tmpBuffer = new byte[ReceiveBytes + newRecvBytes.Length];
                ReceiveBuffer.CopyTo(tmpBuffer, 0);
                ReceiveBuffer = tmpBuffer;
            }

            Array.Copy(newRecvBytes, 0, ReceiveBuffer, ReceiveBytes, newRecvBytes.Length);
            ReceiveBytes += newRecvBytes.Length;
        }

        /// <summary>
        /// 从接收缓冲区头部移除指定长度的数据
        /// </summary>
        /// <param name="count"></param>
        public void RemoveData(int count)
        {
            if (count > ReceiveBytes)
            {
                ReceiveBytes = 0;
            }
            else
            {
                int leftBytes = ReceiveBytes - count;
                byte[] tmpBuffer = new byte[leftBytes];
                Array.Copy(ReceiveBuffer, count, tmpBuffer, 0, leftBytes);
                tmpBuffer.CopyTo(ReceiveBuffer, 0);

                ReceiveBytes = leftBytes;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void DisConnect()
        {
            if (state != ClientState.IsConnected)
            {
                state = ClientState.DisConnected;
                return;
            }
            Socket s = clientSocket;

            if (s != null && s.Connected)
            {
                try
                {
                    s.Close();
                }
                catch (Exception)
                {
                    //client already closed
                }
            }

            state = ClientState.DisConnected;
            //移除事件订阅
            foreach (MySocketAsyncEventArgs mysock in mysockList)
                mysock.Completed -= OnSend;
            listenerSocketAsyncEventArgs.Completed -= OnReceive;
        }

        /// <summary>
        /// 资源释放
        /// </summary>
        public void Dispose()
        {
            DisConnect();
            autoConnectEvent.Close();

            mysockList.Clear();

            state = ClientState.DisConnected;
        }

        #endregion
    }
}
