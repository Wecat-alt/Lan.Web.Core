using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CAT.NsrRadarSdk
{
    class RadarUdp : IRadarSocket
    {
        private UdpClient udpClient;
        private bool disposed;

        public RadarUdp()
        {
            udpClient = null;
            disposed = false;
        }

        public void Dispose()
        {
            if (udpClient == null && disposed)
                return;
            try
            {
                Close();
            }
            catch
            {
            }
            GC.SuppressFinalize(this);
            disposed = true;
        }

        public void Bind(int port)
        {
            CheckDispose();

            if (udpClient != null)
            {
                return;
            }
            UdpClient client = new UdpClient(port, AddressFamily.InterNetwork);
            udpClient = client;
        }

        private void CheckDispose()
        {
            if (disposed)
                throw new ObjectDisposedException("RadarUdp");
        }

        public bool Connect(NsrRadar radar)
        {
            return true;
        }

        public RadarMessage ReceiveOnePacket()
        {
            CheckDispose();

            if (udpClient.Available == 0)
                return null;
            byte[] bufferBytes;
            IPEndPoint remoteEndPoint = null;
            bufferBytes = udpClient.Receive(ref remoteEndPoint);

            RadarMessage radarMessage = new RadarMessage()
            {
                Buffer = bufferBytes,
                Ip = remoteEndPoint.Address.ToString(),
                Length = bufferBytes.Length,
                Port = remoteEndPoint.Port
            };
            return radarMessage;
        }

        public bool Send(RadarMessage sendMessage)
        {
            CheckDispose();

            udpClient.Send(sendMessage.Buffer, sendMessage.Length, sendMessage.Ip, sendMessage.Port);
            return true;
        }

        public void Close(string ip)
        {
            return;
        }

        public void Close()
        {
            CheckDispose();

            var client = udpClient;
            if (client != null)
            {
                client.Close();
                udpClient = null;
            }
        }
    }
}
