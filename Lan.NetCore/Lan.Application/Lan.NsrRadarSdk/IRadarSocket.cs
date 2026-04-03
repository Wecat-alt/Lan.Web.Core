using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAT.NsrRadarSdk
{
    /// <summary>
    /// 雷达Socket接口
    /// </summary>
    interface IRadarSocket : IDisposable
    {
        /// <summary>
        /// 绑定本地端口
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        void Bind(int port);

        /// <summary>
        /// 连接到指定雷达
        /// </summary>
        /// <param name="radar"></param>
        /// <returns></returns>
        bool Connect(NsrRadar radar);

        /// <summary>
        /// 解析一包数据并返回，如果没有则返回null
        /// </summary>
        /// <returns></returns>
        RadarMessage ReceiveOnePacket();

        /// <summary>
        /// 发送一包数据
        /// </summary>
        /// <param name="sendMessage"></param>
        /// <returns></returns>
        bool Send(RadarMessage sendMessage);

        /// <summary>
        /// 关闭指定连接
        /// </summary>
        /// <param name="ip"></param>
        void Close(string ip);

        /// <summary>
        /// 关闭所有连接
        /// </summary>
        void Close();

    }
}
