using System.Runtime.InteropServices;
using System.Text;

namespace CAT.NsrRadarSdk.NsrTypes
{
    /// <summary>
    /// Namespace <c>NsrRadarSdk.NsrTypes</c> contains types and structs that used by the SDK.
    /// <c>NsrRadarSdk.NsrTypes</c>命名空间包含SDK使用的类型和结构体。
    /// </summary>
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
        //用于提供命名空间注释的类
    }

    #region 内部
    /// <summary>
    /// 协议相关错误码
    /// </summary>
    internal enum RVS_ERROR : int
    {
        RVS_SOFEOPEN = 1, // 上位机软件打开
        RVS_OK = 0,
        RVS_ERR_NOHEAD = -1, // 没有帧头
        RVS_ERR_LEN = -2, // 帧长度错误
        RVS_ERR_SRCADDR = -3, // 源地址错误
        RVS_ERR_DETADDR = -4, // 目标地址错误
        RVS_ERR_CHECKCODE = -5, // 校验码错误
        RVS_ERR_COMMADN = -6, // 命令不存在
        RVS_ERR_NOTFRAME = -7, // 非完整的帧

        RVS_ERR_OTHER = -8, // 其他错误
    }


    /// <summary>
    /// 雷达通信协议命令
    /// </summary>
    internal enum RVS_COMMAND : byte
    {
        NULL,
        RVS_RESETCOMMAND = 0x01, // 恢复出厂设置
        RVS_BUZZERCOMMAND = 0x02, // 蜂鸣器设置
        RVS_COORDINATECOMMAND = 0x03, // 坐标设置
        RVS_NETCOMMAND = 0x04, // 网络设置
        RVS_HEARTBEATCOMMAND = 0x09, // 心跳设置
        RVS_STATUSREADCOMMAND = 0x0A, // 读取状态
        RVS_ADDRESSCOMMAND = 0x0B, // 地址编码设置
        RVS_INSTALLPARAMCOMMAND = 0x20, // 安装参数设置
        RVS_SAMPLEPARAMCOMMAND = 0x21, // 采样参数设置 
        RVS_ALGORITHMCOMMAND = 0x22, // 雷达算法参数
        RVS_SAVERAWDATACOMMAND = 0x23, // 保存数据
        RVS_SYSTEMTIMECOMMAND = 0x24, // 雷达系统时间
        RVS_SOFTFUNCCOMMAND = 0x25, // 软件功能设置
        RVS_FCTADCTESTCOMMAND = 0x30, // 2.12	FCT ADC性能测试
        RVS_TranTESTCOMMAND = 0x31, // 2.13	暗箱发射机性能测试
        RVS_SAVEPARACOMMAND = 0x88, // 保存参数
        RVS_ANSWERCOMMAND = 0xA2, // 返回帧命令
        RVS_HEARTCOMMAND = 0xA4, // 心跳包
        RVS_TARGETSENDCOMMAND_A8 = 0xA8, // 其他雷达目标协议
        RVS_TARGETSENDCOMMAND_A9 = 0xA9, // 800W 点云 目标发送
        RVS_BROADCASTCOMMAND = 0xFF, // 雷达广播命令
    }

    /// <summary>
    /// 协议帧结构
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal class rvs_FRAME_Context
    {
        public RVS_DeviceAddress srcAddr; // 源地址
        public RVS_DeviceAddress dstAddr; // 目的地址
        //public RVS_DeviceAddressNEW typenew;
        public RVS_COMMAND command; // 命令
        public UInt16 paramLen; // 参数长度

        /// <summary>
        /// params, use for unpack
        /// </summary>
        public byte[] paramBytes; // 参数内容起始指针,根据命令，分配内容，填充数据。
        /// <summary>
        /// params, use for make packet
        /// </summary>
        public object paramObject;

        /// <summary>
        /// Check the command's result if this if a received packet
        /// </summary>
        public bool OperationSucceed
        {
            get
            {
                byte[] bufferBytes = paramBytes;
                if (bufferBytes == null)
                {
                    return false;
                }
                if (bufferBytes.Length == 2)
                {
                    const byte successFlag = 0x0F;
                    if (bufferBytes[1] == successFlag)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (command == RVS_COMMAND.RVS_STATUSREADCOMMAND)
                {
                    return true;
                }
                else
                {
                    return true;
                }
            }
        }

        public rvs_FRAME_Context()
        {
            command = RVS_COMMAND.NULL;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(100);
            sb.Append(string.Format("SrcAddr: {0} , DestAddr: {1} , CMD: {2} , ParamLen: {3}\r\n"
                , srcAddr, dstAddr, command, paramLen));
            byte[] buffer = null;
            if (paramObject is IntPtr)
            {
                buffer = new byte[paramLen];
                Marshal.Copy((IntPtr)paramObject, buffer, 0, paramLen);

            }
            else
            {
                buffer = paramBytes;
            }
            if (buffer != null)
            {
                foreach (byte ch in buffer)
                {
                    sb.Append(ch.ToString("x2"));
                    sb.Append(" ");
                }
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// 协议基本元素 eg:坐标点表示 eg: 例：-2500.3表示为1000 0011 0000 1001 1100 0100
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct RVS_PARAM_BASIC
    {
        public byte sign; // 符号位 最高位为符号位（0为正，1为负），0~3位为小数位
        public byte high_8; // 高8位
        public byte low_8; // 低8位 

        public float Value { get { return RadarProtocol.Convert(ref this); } }

        public static RVS_PARAM_BASIC FromFloat(float value)
        {
            RVS_PARAM_BASIC ret = RadarProtocol.Convert(value);
            return ret;
        }
    }

    /// <summary>
    /// 方向速度
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct RVS_PARAM_SPEED
    {
        public byte sign; // 最高位为符号位（0位正，1为负）0~3位为小数位
        public byte speed; // 为速度整数位

        public float Value
        {
            get
            {
                float num = speed;
                num += (float)(sign & 0x0F) * 0.1f;
                if ((sign & 0x80) != 0)
                    num = -num;
                return num;
            }
        }

        public static RVS_PARAM_SPEED FromFloat(float value)
        {
            RVS_PARAM_SPEED param = new RVS_PARAM_SPEED();

            if (value < 0)
            {
                param.sign = 0x80;
                value = -value;
            }
            else
            {
                param.sign = 0;
            }

            int numInt = (int)(value * 10);
            param.sign |= (byte)(numInt % 10);

            numInt /= 10;
            param.speed = (byte)numInt;

            return param;
        }
    }

    /// <summary>
    /// 应答帧参数
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct RVS_PARAM_ANSWER
    {
        //public RVS_COMMAND command; // 对应的命令码
        public byte answerRet; // 0xF0 - 执行不成功  0x0F - 执行成功
    }

    /// <summary>
    /// 心跳应答帧参数
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct RVS_PARAM_HEART_ANSWER
    {
        public RVS_COMMAND command; // 对应的命令码
        public byte answerRet; // 0xF0 - 执行不成功  0x0F - 执行成功

        public static RVS_PARAM_HEART_ANSWER GetDefault()
        {
            RVS_PARAM_HEART_ANSWER answer = new RVS_PARAM_HEART_ANSWER();
            answer.command = RVS_COMMAND.RVS_HEARTCOMMAND;
            answer.answerRet = 0x0F;

            return answer;
        }
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct RVS_PARAM_NET
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] IP; // IP地址 ：xxx：xxx：xxx：xxx
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] NetMask; // NetMask：xxx：xxx：xxx：xxx
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] GateWay; // GateWay：xxx：xxx：xxx：xxx 

        public RVS_PARAM_NET(System.Net.IPAddress ip, System.Net.IPAddress netmask, System.Net.IPAddress gateway)
        {
            IP = new byte[4];
            NetMask = new byte[4];
            GateWay = new byte[4];

            ip.GetAddressBytes().CopyTo(IP, 0);
            netmask.GetAddressBytes().CopyTo(NetMask, 0);
            gateway.GetAddressBytes().CopyTo(GateWay, 0);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct RVS_PORT
    {
        public ushort port; // 端口 
    }

    //防区区域坐标点
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct RVS_LOCATION
    {
        public float x;
        public float y;
    }

    // 完整的目标信息
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct rvs_TARGET_INFO_v100 : IRvs_Target
    {
        byte targetId; // 目标ID范围0x01~0x40 
        byte targetLen; // 目标长度高4位为整数位，低4位为小数位
        RVS_PARAM_SPEED speed_X; // X方向速度
        RVS_PARAM_SPEED speed_Y; // Y方向速度
        RVS_PARAM_BASIC cod_X; // X方向坐标
        RVS_PARAM_BASIC cod_Y; // Y方向坐标 
        RVS_PARAM_BASIC distance; // 目标距离
        RVS_PARAM_BASIC azimuth; // 目标方位角
        RVS_PARAM_BASIC SNR; // 目标SNR 
        RVS_PARAM_BASIC EN; // 目标峰值能量

        public uint Id { get { return targetId; } }
        public uint Type { get { return 0; } }

        public float X
        {
            get { return RadarProtocol.Convert(ref cod_X); }
        }

        public float Y
        {
            get { return RadarProtocol.Convert(ref cod_Y); }
        }

        public float SpeedX
        {
            get { return speed_X.Value; }
        }

        public float SpeedY
        {
            get { return speed_Y.Value; }
        }

        public float SpeedZ { get { return 0; } }
        public float AxesX { get { return X; } }
        public float AxesY { get { return Y; } }
        public float AxesZ { get { return 0; } }

        public float Distance
        {
            get { return distance.Value; }
        }

        public float AzimuthAngle
        {
            get { return azimuth.Value; }
        }

        public float ElevationAngle { get { return 0; } }
        public float Snr { get { return 0; } }
        public float PeakEnergy { get { return 0; } }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct rvs_TARGET_INFO_v101 : IRvs_Target
    {
        uint id;
        uint type;
        float speedX;
        float speedY;
        float speedZ;
        float axesX;
        float axesY;
        float axesZ;
        float distance;
        float azimuthAngle;
        float elevationAngle;
        float snr;
        float peakEnergy;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
        private byte[]
            reserveBytes;

        public uint Id
        {
            get { return id; }
        }

        public uint Type
        {
            get { return type; }
        }

        public float X
        {
            get { return axesX; }
        }

        public float Y
        {
            get { return axesY; }
        }

        public float SpeedX
        {
            get { return speedX; }
        }

        public float SpeedY
        {
            get { return speedY; }
        }

        public float SpeedZ
        {
            get { return speedZ; }
        }

        public float AxesX
        {
            get { return axesX; }
        }

        public float AxesY
        {
            get { return axesY; }
        }

        public float AxesZ
        {
            get { return axesZ; }
        }

        public float Distance
        {
            get { return distance; }
        }

        public float AzimuthAngle
        {
            get { return azimuthAngle; }
        }

        public float ElevationAngle
        {
            get { return elevationAngle; }
        }

        public float Snr
        {
            get { return snr; }
        }

        public float PeakEnergy
        {
            get { return peakEnergy; }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct rvs_TARGET_INFO_v010 : IRvs_Target
    {
        private byte targetId;
        private byte targetLen;
        private RVS_PARAM_SPEED speed_X;
        private RVS_PARAM_SPEED speed_Y;
        private RVS_PARAM_BASIC cod_X;
        private RVS_PARAM_BASIC cod_Y;
        private RVS_PARAM_BASIC snr;
        private RVS_PARAM_BASIC peakenergy;

        public uint Id
        {
            get { return (uint)this.targetId; }
        }

        public uint Type
        {
            get { return 0; }
        }

        public float X
        {
            get { return RadarProtocol.Convert(ref this.cod_X); }
        }

        public float Y
        {
            get { return RadarProtocol.Convert(ref this.cod_Y); }
        }

        public float SpeedX
        {
            get { return this.speed_X.Value; }
        }

        public float SpeedY
        {
            get { return this.speed_Y.Value; }
        }

        public float SpeedZ
        {
            get { return 0.0f; }
        }

        public float AxesX
        {
            get { return this.X; }
        }

        public float AxesY
        {
            get { return this.Y; }
        }

        public float AxesZ
        {
            get { return 0.0f; }
        }

        public float Distance
        {
            get { return (float)Math.Sqrt((double)this.X * (double)this.X + (double)this.Y * (double)this.Y); }
        }

        public float AzimuthAngle
        {
            get { return (float)(Math.Atan2((double)this.Y, (double)this.X) * 180.0 / Math.PI); }
        }

        public float ElevationAngle
        {
            get { return 0.0f; }
        }

        public float Snr
        {
            //get { return 0.0f; }
            get { return RadarProtocol.Convert(ref this.snr); }
        }

        public float PeakEnergy
        {
            get { return RadarProtocol.Convert(ref this.peakenergy); }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct RVS_PARAM_TARGET
    {
        public byte targetNum; // 目标个数n >=1
        //public IntPtr targetInfo;    // 目标信息点数组的指针，数目为targetNum        
    }

    #endregion

    #region 公开
    /// <summary>
    /// Radar target info
    /// </summary>
    public interface IRvs_Target
    {
        /// <summary>
        /// Target id
        /// </summary>
        uint Id { get; }
        /// <summary>
        /// Target type (reserved)
        /// </summary>
        uint Type { get; }
        /// <summary>
        /// X方向坐标（m）
        /// </summary>
        float X { get; }
        /// <summary>
        /// Y方向坐标（m）
        /// </summary>
        float Y { get; }
        /// <summary>
        /// X方向速度（m/s）
        /// </summary>
        float SpeedX { get; }
        /// <summary>
        /// Y方向速度（m/s）
        /// </summary>
        float SpeedY { get; }
        /// <summary>
        /// Z方向速度（m/s）
        /// </summary>
        float SpeedZ { get; }
        /// <summary>
        /// X方向坐标（m）
        /// </summary>
        float AxesX { get; }
        /// <summary>
        /// Y方向坐标（m）
        /// </summary>
        float AxesY { get; }
        /// <summary>
        /// Z方向坐标（m）
        /// </summary>
        float AxesZ { get; }
        /// <summary>
        /// 目标距离（m）
        /// </summary>
        float Distance { get; }
        /// <summary>
        /// 方位角（度）
        /// </summary>
        float AzimuthAngle { get; }
        /// <summary>
        /// 俯仰角（度）
        /// </summary>
        float ElevationAngle { get; }
        /// <summary>
        /// 信噪比（保留的）
        /// </summary>
        float Snr { get; }
        /// <summary>
        /// 峰值能量（保留的）
        /// </summary>
        float PeakEnergy { get; }
    }

    /// <summary>
    /// Radar target list class
    /// </summary>
    public class RVS_Target_List
    {
        private int version = 100;
        private IRvs_Target[] targets;

        /// <summary>
        /// 列表中包含的目标数量
        /// </summary>
        public int TargetNum { get; set; }

        /// <summary>
        /// 目标列表
        /// </summary>
        public IRvs_Target[] Targets
        {
            get { return targets; }
            private set { targets = value; }
        }

        //public bool Outdated { get; set; }

#if false
        public RVS_Target_List(RVS_PARAM_TARGET targets, IntPtr ptr)
        {
            TargetNum = targets.targetNum;
            //IntPtr ptr = new IntPtr(targets.targetInfo);
            ptr = IntPtr.Add(ptr, Marshal.SizeOf(typeof(RVS_PARAM_TARGET)));
            RVS_TARGET_INFO[] targetsArray = new RVS_TARGET_INFO[TargetNum];
            for (int i = 0; i < TargetNum; i++)
            {
                RVS_TARGET_INFO item = (RVS_TARGET_INFO) Marshal.PtrToStructure(ptr, typeof(RVS_TARGET_INFO));

                //     item.X = RadarProtocol.Convert(ref item.cod_X);
                //    item.Y = RadarProtocol.Convert(ref item.cod_Y);
                targetsArray[i] = item;
                ptr = IntPtr.Add(ptr, Marshal.SizeOf(typeof(RVS_TARGET_INFO)));
            }
            Targets = targetsArray;
            //Outdated = false;
        }
#endif
        internal RVS_Target_List(byte[] buf, string type)
        {
            if (type == "A8")
            {
                #region 其他雷达
                TargetNum = buf[0];

                if (TargetNum == 0)
                    throw new ArgumentException("目标参数长度为0");

                int targetTypeSize = (buf.Length - 1) / TargetNum;

                if (targetTypeSize == Marshal.SizeOf(typeof(rvs_TARGET_INFO_v100)))
                {
                    version = 100;
                }
                else if (targetTypeSize == Marshal.SizeOf(typeof(rvs_TARGET_INFO_v101)))
                {
                    version = 101;
                }
                else if (targetTypeSize == Marshal.SizeOf(typeof(rvs_TARGET_INFO_v010)))
                {
                    version = 10;
                }
                else
                {
                    throw new ArgumentException("目标参数长度错误");
                }

                IRvs_Target[] target = new IRvs_Target[TargetNum];
                IntPtr ptrBuf = IntPtr.Zero;

                try
                {
                    ptrBuf = Marshal.AllocHGlobal(buf.Length - 1);
                    if (ptrBuf == IntPtr.Zero)
                        throw new Exception();
                    Marshal.Copy(buf, 1, ptrBuf, buf.Length - 1);

                    IntPtr ptr = ptrBuf;
                    if (version == 100)
                    {
                        for (int i = 0; i < TargetNum; i++)
                        {
                            rvs_TARGET_INFO_v100 item =
                                (rvs_TARGET_INFO_v100)Marshal.PtrToStructure(ptr, typeof(rvs_TARGET_INFO_v100));

                            target[i] = item;
                            ptr = IntPtr.Add(ptr, Marshal.SizeOf(typeof(rvs_TARGET_INFO_v100)));
                        }
                    }
                    else if (version == 101)
                    {
                        for (int i = 0; i < TargetNum; i++)
                        {
                            rvs_TARGET_INFO_v101 item =
                                (rvs_TARGET_INFO_v101)Marshal.PtrToStructure(ptr, typeof(rvs_TARGET_INFO_v101));

                            target[i] = item;
                            ptr = IntPtr.Add(ptr, Marshal.SizeOf(typeof(rvs_TARGET_INFO_v101)));
                        }
                    }
                    else if (this.version == 10)
                    {

                        for (int i = 0; i < this.TargetNum; ++i)
                        {
                            rvs_TARGET_INFO_v010 structure =
                                (rvs_TARGET_INFO_v010)Marshal.PtrToStructure(ptr, typeof(rvs_TARGET_INFO_v010));
                            target[i] = (IRvs_Target)structure;
                            ptr = IntPtr.Add(ptr, Marshal.SizeOf(typeof(rvs_TARGET_INFO_v010)));
                        }
                    }
                    else
                    {
                        throw new ArgumentException("结构体版本错误");
                    }
                    Targets = target;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (ptrBuf != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(ptrBuf);
                    }
                }
                #endregion
            }
            else
            {
                #region A9雷达
                byte[] bTargetNum = new byte[4];
                bTargetNum[0] = buf[0];
                bTargetNum[1] = buf[1];
                bTargetNum[2] = buf[2];
                bTargetNum[3] = buf[3];


                int iTargetNum = BitConverter.ToInt32(bTargetNum, 0);
                TargetNum = iTargetNum;

                if (TargetNum == 0)
                    throw new ArgumentException("目标参数长度为0");

                int targetTypeSize = (buf.Length - 4) / TargetNum;

                if (targetTypeSize == Marshal.SizeOf(typeof(rvs_TARGET_INFO_v100)))
                {
                    version = 100;
                }
                else if (targetTypeSize == Marshal.SizeOf(typeof(rvs_TARGET_INFO_v101)))
                {
                    version = 101;
                }
                else if (targetTypeSize == Marshal.SizeOf(typeof(rvs_TARGET_INFO_v010)))
                {
                    version = 10;
                }
                else
                {
                    throw new ArgumentException("目标参数长度错误");
                }

                IRvs_Target[] target = new IRvs_Target[TargetNum];
                IntPtr ptrBuf = IntPtr.Zero;

                try
                {
                    ptrBuf = Marshal.AllocHGlobal(buf.Length - 4);
                    if (ptrBuf == IntPtr.Zero)
                        throw new Exception();
                    Marshal.Copy(buf, 4, ptrBuf, buf.Length - 4);

                    IntPtr ptr = ptrBuf;
                    if (version == 100)
                    {
                        for (int i = 0; i < TargetNum; i++)
                        {
                            rvs_TARGET_INFO_v100 item =
                                (rvs_TARGET_INFO_v100)Marshal.PtrToStructure(ptr, typeof(rvs_TARGET_INFO_v100));

                            target[i] = item;
                            ptr = IntPtr.Add(ptr, Marshal.SizeOf(typeof(rvs_TARGET_INFO_v100)));
                        }
                    }
                    else if (version == 101)
                    {
                        for (int i = 0; i < TargetNum; i++)
                        {
                            rvs_TARGET_INFO_v101 item =
                                (rvs_TARGET_INFO_v101)Marshal.PtrToStructure(ptr, typeof(rvs_TARGET_INFO_v101));

                            target[i] = item;
                            ptr = IntPtr.Add(ptr, Marshal.SizeOf(typeof(rvs_TARGET_INFO_v101)));
                        }
                    }
                    else if (this.version == 10)
                    {

                        for (int i = 0; i < this.TargetNum; ++i)
                        {
                            rvs_TARGET_INFO_v010 structure =
                                (rvs_TARGET_INFO_v010)Marshal.PtrToStructure(ptr, typeof(rvs_TARGET_INFO_v010));
                            target[i] = (IRvs_Target)structure;
                            ptr = IntPtr.Add(ptr, Marshal.SizeOf(typeof(rvs_TARGET_INFO_v010)));
                        }
                    }
                    else
                    {
                        throw new ArgumentException("结构体版本错误");
                    }
                    Targets = target;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (ptrBuf != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(ptrBuf);
                    }
                }
                #endregion
            }
        }
    }

    /// <summary>
    /// Radar device address
    /// </summary>
    public enum RVS_DeviceAddress : byte
    {
        Unknown = 0,
        PC_ADDR = 0x10, // 上位机地址
        RADAR_ADDR = 0x40, // 雷达地址
        WVF_RADAR_100_ADDR = 0x60, //SP100W雷达地址
        WVF_RADAR_50_ADDR = 0x70, //SP50W雷达地址 
        LASER_ADDR = 0x80,
        WVF_RADAR_300_ADDR = 0x90, //SP300W雷达地址
        BroadCast = 0xFF,




    }

    public enum RVS_DeviceAddressNEW : byte
    {
        Unknown = 0,
        PC_ADDR = 0x10, // 上位机地址
        //RADAR_ADDR = 0x40, // 雷达地址

        RADAR_ADDR = 0x40, // 雷达地址
        RADAR_ADDR_new = 0x01, // 雷达地址

        WVF_RADAR_100W_ADDR = 0x60, //SP100W雷达地址
        WVF_RADAR_100W_new_ADDR = 0x02, //SP100W雷达地址

        WVF_RADAR_300W_ADDR = 0x90, //SP300W雷达地址
        WVF_RADAR_300W_new_ADDR = 0x04, //SP300W雷达地址
        WVF_RADAR_300_M_ADDR = 0x10, //SP300W雷达地址

        WVF_RADAR_100W_LD_ADDR = 0x07, //SP100W_LD雷达地址
        WVF_RADAR_100W_M_ADDR = 0x08, //SP100W雷达地址

        WVF_RADAR_NSR50W_ADDR = 0x70, //SP50W雷达地址 
        WVF_RADAR_50W_new_ADDR = 0x03, //SP50W雷达地址 

        WVF_RADAR_NSR200_ADDR = 0x17, //nsr200 
        WVF_RADAR_NSR120_ADDR = 0x1D, //nsr120 

        WVF_RADAR_NSR150_ADDR = 0x09, //nsr 150


        WVF_RADAR_NSR60W_ADDR = 0x12, //nsr60w

        WVF_RADAR_WTC261_3000_800_ADDR = 0x18, //800测船

        LASER_ADDR = 0x80,
        BroadCast = 0xFF,
    }
    /// <summary>
    /// Type of radar
    /// </summary>
    public enum RadarType
    {
        Unknown,
        SP100,
        SP150,
        SP200,
        SP120,
        SP100W,
        SP100W_M,
        SP50W,
        SP300W,
        SP300W_M,
        SP60W,
        SP800,
        Laser = 0x20,
    }

    /// <summary>
    /// Buzzer state
    /// 蜂鸣器状态
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct RVS_PARAM_BUZZERSET
    {

        internal byte buzzerSet; // 0xA0 - 打开		0xA2 - 关闭

        /// <summary>
        /// 获取或设置蜂鸣器启用状态
        /// </summary>
        public bool IsOpen
        {
            get { return buzzerSet == 0xA0; }
            set { buzzerSet = value ? (byte)0xA0 : (byte)0xA2; }
        }
    }

    /// <summary>
    /// Filter coordinate
    /// 过滤规则坐标点
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct RVS_PARAM_COORDINATE
    {
        /// <summary>
        /// No. 
        /// 坐标序号
        /// </summary>
        public byte coordinateNo;
        internal RVS_PARAM_BASIC coordinate_X; // X        
        internal RVS_PARAM_BASIC coordinate_Y; // Y

        /// <summary>
        /// X方向坐标（m）
        /// </summary>
        public float X
        {
            get { return RadarProtocol.Convert(ref coordinate_X); }
            set { coordinate_X = RadarProtocol.Convert(value); }
        }

        /// <summary>
        /// Y方向坐标（m）
        /// </summary>
        public float Y
        {
            get { return RadarProtocol.Convert(ref coordinate_Y); }
            set { coordinate_Y = RadarProtocol.Convert(value); }
        }
    }

    /// <summary>
    /// Heart time
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct RVS_PARAM_HEART
    {
        /// <summary>
        /// heart time value (s)
        /// </summary>
        public byte time; // 心跳时间 
    }


    /// <summary>
    /// TIME COMMAND
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct RVS_SYSTEMTIMECOMMAND
    {
        /// <summary>
        /// heart time value (s)
        /// </summary>
        public byte Y; // 年
        public byte M; // 月
        public byte D; // 日 
        public byte H; // 时 
        public byte m; // 分 
        public byte S; // 秒 
    }


    /*  firmwareVer[2]
    **  firmwareVer[0]固件版本 主版本号【高4位】 + 次版本号【低4位】  
    ** 	firmwareVer[1]固件版本 阶段版本号 有效值0~255
    **  deviceType[2]
    **  deviceType[0]设备类型 
    **  deviceType[1]保留
    */

    /// <summary>
    /// Radar broadcast info
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct RVS_PARAM_BROADCAST
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        internal byte[] firmwareVer;
        // 附件版本 固件版本 主版本号【高4位】 + 次版本号【低4位】 , 固件版本 阶段版本号 有效值0~255  

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        internal byte[] deviceType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        internal byte[] macAddr;

        /// <summary>
        /// Firmware version
        /// </summary>
        public string FirmwareVersion
        {
            get
            {
                return string.Format("{0}.{1}.{2}",
                    (firmwareVer[0] & 0xF0) >> 4, (firmwareVer[0] & 0x0F), firmwareVer[1]);
            }
        }

        /// <summary>
        /// Internal device type number
        /// </summary>
        public string DeviceType
        {
            get { return deviceType[0].ToString(); }
        }

        /// <summary>
        /// Mac address
        /// </summary>
        public string MacAddress
        {
            get
            {
                StringBuilder sb = new StringBuilder(3 * macAddr.Length);
                for (int i = 0; i < macAddr.Length; i++)
                {
                    sb.AppendFormat("{0:X2}-", macAddr[i]);
                }
                sb.Remove(sb.Length - 1, 1);

                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// Radar version info
    /// 雷达版本信息
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct RVS_RadarVersion
    {
        internal ushort armSoftVersion;
        internal ushort fpgaSoftVersion;
        internal ushort radarAlgorithm;

        /// <summary>
        /// Firmware version
        /// </summary>
        public string FirmwareVersion
        {
            get { return FormatValue(armSoftVersion); }
        }

        /// <summary>
        /// Algorithm version
        /// </summary>
        public string AlgorithmVersion
        {
            get { return FormatValue(radarAlgorithm); }
        }

        /// <summary>
        /// Fpga version
        /// </summary>
        public string FpgaVersion
        {
            get { return FormatValue(fpgaSoftVersion); }
        }

        private string FormatValue(ushort val)
        {
            return string.Format("{0}.{1}.{2}",
                (val & 0xF0) >> 4, (val & 0x0F), (val & 0xFF00) >> 8);
        }

    }


    //雷达型号 0x0001:sp100; 0x0002:sp100VF; 0x0003:sp100WVF
    /// <summary>
    /// Radar's internal number
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct rvs_RadarType
    {
        /// <summary>
        /// number value
        /// </summary>
        public short radarType;
    }


    /// <summary>
    /// Radar status info, response for RVS_STATUSREADCOMMAND.
    /// 雷达状态结构，雷达RVS_STATUSREADCOMMAND命令，确认帧的内容结构
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct rvs_PARAM_STATUS
    {
        //public RVS_COMMAND command;                 // 对应的命令码
        /// <summary>
        /// Radar address
        /// </summary>
        public byte addr; // 雷达本地地址
        /// <summary>
        /// Heart time
        /// </summary>
        public RVS_PARAM_HEART heart; // 雷达心跳
        /// <summary>
        /// Buzzer state
        /// </summary>
        public RVS_PARAM_BUZZERSET bee; // 蜂鸣器状态
        /// <summary>
        /// Radar version info
        /// </summary>
        public RVS_RadarVersion radarVerInfo; // 雷达版本信息
        /// <summary>
        /// Radar internal number
        /// </summary>
        public rvs_RadarType radarTypeInfo; // 雷达型号
        /// <summary>
        /// Filter coordinate
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public RVS_PARAM_COORDINATE[] position; // 雷达坐标信息
    }

    #endregion
}
