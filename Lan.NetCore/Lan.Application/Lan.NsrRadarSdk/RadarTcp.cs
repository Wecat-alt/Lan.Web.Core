using CAT.NsrRadarSdk.NsrTypes;
using CAT.NsrRadarSdk.SocketManage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAT.NsrRadarSdk
{
    /// <summary>
    /// 雷达TCP连接管理
    /// </summary>
    class RadarTcp : IRadarSocket
    {
        private readonly ConcurrentDictionary<string, SocketClient> _clients;
        private Dictionary<string, List<byte>> _receivedBytes;
        private bool disposed;
        public RadarTcp()
        {
            _clients = new ConcurrentDictionary<string, SocketClient>();
            _receivedBytes = new Dictionary<string, List<byte>>();
            disposed = false;
        }

        private void CheckDispose()
        {
            if (disposed)
                throw new ObjectDisposedException("RadarTcp");
        }

        public void Dispose()
        {
            try
            {
                Close();
                _receivedBytes.Clear();
            }
            catch
            {

            }
            GC.SuppressFinalize(this);
            disposed = true;
        }

        public void Bind(int port)
        {
            throw new NotImplementedException();
        }

        public bool Connect(NsrRadar radar)
        {
            CheckDispose();

            SocketClient tcpClient = null;
            if (_clients.TryGetValue(radar.Ip, out tcpClient) && tcpClient.IsConnected)
            {
                return true;
            }
            if (null == tcpClient)
            {
                tcpClient = new SocketClient(radar);
                tcpClient.OnDataReceived += OnDataReceived;
                tcpClient.OnServerStoped += OnServerStoped;
            }

            bool isConnected = tcpClient.Connect();

            if (isConnected)
            {
                _clients[radar.Ip] = tcpClient;
            }
            return isConnected;
        }

        public void Close(string ip)
        {
            SocketClient tcpClient;
            if (_clients.TryRemove(ip, out tcpClient) && tcpClient != null)
            {
                tcpClient.OnDataReceived -= OnDataReceived;
                tcpClient.OnServerStoped -= OnServerStoped;
                tcpClient.Dispose();
            }
        }

        public void Close()
        {
            var clients = _clients.Values.ToArray();
            if (clients.Length == 0)
                return;
            _clients.Clear();
            Parallel.ForEach(clients, tcpClient =>
            {
                try
                {
                    tcpClient.OnDataReceived -= OnDataReceived;
                    tcpClient.OnServerStoped -= OnServerStoped;
                    tcpClient.Dispose();
                }
                catch
                {

                }
            });
        }
        private void OnServerStoped(SocketClient socketClient)
        {
            try
            {
                NsrRadar radar = socketClient.Radar;

                radar.Online = false;
            }
            catch (Exception)
            {

            }
        }

        private void OnDataReceived(SocketClient socketClient, byte[] bytes)
        {
            socketClient.AppendData(bytes);
            while (true)
            {
                int packetLen = socketClient.ReceiveBytes;
                byte[] myReadBuffer = socketClient.ReceiveBuffer;
                RVS_ERROR err = RadarProtocol.rvs_checkIsCompleteFrame(myReadBuffer, ref packetLen);
                if (err == RVS_ERROR.RVS_ERR_LEN)
                {
                    break;
                }
                else if (err != RVS_ERROR.RVS_OK)
                {
                    socketClient.RemoveData(packetLen);
                    continue;
                }

                byte[] bufferBytes = new byte[packetLen];
                Array.Copy(myReadBuffer, bufferBytes, packetLen);

                socketClient.RemoveData(packetLen);
                //雷达解析数据
                NsrRadar radar = socketClient.Radar;
                rvs_FRAME_Context context = radar.ReceiveData(NsrSdk.Instance, bufferBytes);

                if (context.command != RVS_COMMAND.NULL)
                {
                    byte tt = context.paramBytes[9];
                    //radar.DevAddress = (RVS_DeviceAddressNEW)tt; //context.srcAddr;
                    radar.DevAddressNew = (RVS_DeviceAddressNEW)tt; //context.srcAddr;
                    radar.Online = true;
                }
                if (context.command == RVS_COMMAND.RVS_TARGETSENDCOMMAND_A8 || context.command == RVS_COMMAND.RVS_TARGETSENDCOMMAND_A9) //解析包，如果是报警目标上传信息，则引发事件
                {
                    NsrSdk.Instance.OnTargetDetect(radar, context.paramObject as RVS_Target_List);
                }
            }
        }

        public RadarMessage ReceiveOnePacket()
        {
            throw new NotImplementedException();
        }

        public bool Send(RadarMessage sendMessage)
        {
            CheckDispose();

            SocketClient tcpClient;
            if (!_clients.TryGetValue(sendMessage.Ip, out tcpClient))
            {
                throw new RadarException("Radar hasn't been connected.");
            }

            tcpClient.Send(sendMessage.Buffer, sendMessage.Length);
            return true;
        }

    }
}
