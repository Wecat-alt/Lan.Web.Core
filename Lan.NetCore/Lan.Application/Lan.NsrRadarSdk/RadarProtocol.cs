using CAT.NsrRadarSdk.NsrTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CAT.NsrRadarSdk
{
    internal class RadarProtocol
    {
        public const int NormalPacketLen = 8;

        // 协议头
        private const byte RVS_HEARD_0 = 0XA5;
        private const byte RVS_HEARD_1 = 0X5A;

        private const int FRAMEHEADLEN = 2; // 帧头长度
        private const int SRCADDRLNE = 1; // 源地址长度
        private const int DESTADDRLEN = 1; // 目的地址长度  
        private const int COMMANDLEN = 1; // 命令长度  
        private const int PARAMLEN = 2; // 长度长度
        private const int CHECKLEN = 1;

        // 除去参数内容的帧长度
        public const int FRAMELNETOPARAMCONTENT =
            (FRAMEHEADLEN + SRCADDRLNE + DESTADDRLEN + COMMANDLEN + PARAMLEN + CHECKLEN);

        // 帧最大长度
        private const int FRAMEMAXLEN = 1024;
        // 支持坐标点数
        private const int COORDINATENUMMAX = 4;

        /// <summary>
        /// 将
        /// </summary>
        /// <param name="rvs_param"></param>
        /// <returns></returns>
        internal static float Convert(ref RVS_PARAM_BASIC rvs_param)
        {
            float num = rvs_param.low_8 + ((int)rvs_param.high_8 << 8);
            num += (float)(rvs_param.sign & 0x0F) * 0.1f;
            if ((rvs_param.sign & 0x80) != 0)
                num = -num;
            return num;
        }

        internal static RVS_PARAM_BASIC Convert(float num)
        {
            RVS_PARAM_BASIC param = new RVS_PARAM_BASIC();

            if (num < 0)
            {
                param.sign = 0x80;
                num = -num;
            }
            else
            {
                param.sign = 0;
            }

            int numInt = (int)(num * 10);
            param.sign |= (byte)(numInt % 10);

            numInt /= 10;
            param.low_8 = (byte)numInt;
            param.high_8 = (byte)(numInt >> 8);

            return param;
        }

        public static object BytesToStructure(byte[] buf, Type structType)
        {
            IntPtr ptr = IntPtr.Zero;
            try
            {
                int len = Marshal.SizeOf(structType);
                ptr = Marshal.AllocHGlobal(len);
                Marshal.Copy(buf, 0, ptr, len);
                object value = Marshal.PtrToStructure(ptr, structType);
                return value;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }

        public static string BytesToString(byte[] bufferBytes)
        {
            StringBuilder sb = new StringBuilder(bufferBytes.Length * 3);
            foreach (byte num in bufferBytes)
            {
                sb.Append(num.ToString("X2"));
                sb.Append(" ");
            }

            return sb.ToString();
        }

        public static int rvs_FormStatusPacket(byte[] pBuf, int pBuflen)
        {
            rvs_FRAME_Context context = null;
            int packetLength = 0;

            if (pBuf.Length == 0)
            {
                Console.WriteLine("NULL pointer rvs_FormStatusPacket\r\n");
                return (int)RVS_ERROR.RVS_ERR_OTHER;
            }

            context = new rvs_FRAME_Context();

            context.srcAddr = RVS_DeviceAddress.PC_ADDR;
            context.dstAddr = RVS_DeviceAddress.BroadCast;
            context.command = RVS_COMMAND.RVS_STATUSREADCOMMAND;
            context.paramBytes = null;
            context.paramLen = 0;

            packetLength = rvs_FormRVSPacket(pBuf, pBuflen, context);

            return packetLength;
        }

        public static int rvs_FormRVSPacket(byte[] pBuf, int pBuflen, rvs_FRAME_Context pData)
        {
            byte[] answer = new byte[FRAMEMAXLEN];
            int tempLen = 0;

            if (0 == pBuf.Length)
            {
                Console.WriteLine("NULL pointer rvs_FormRVSPacket\r\n");
                return (int)RVS_ERROR.RVS_ERR_OTHER;
            }

            answer[0] = RVS_HEARD_0; // InsertHead
            answer[1] = RVS_HEARD_1;
            answer[2] = (byte)pData.srcAddr;
            answer[3] = (byte)pData.dstAddr;
            answer[4] = (byte)pData.command;
            answer[5] = (byte)pData.paramLen; // 获取内容长度
            answer[6] = (byte)(pData.paramLen >> 8);
            switch (pData.command)
            {
                case RVS_COMMAND.RVS_RESETCOMMAND: //  恢复出厂设置 
                case RVS_COMMAND.RVS_BUZZERCOMMAND:
                case RVS_COMMAND.RVS_COORDINATECOMMAND:
                case RVS_COMMAND.RVS_NETCOMMAND:
                case RVS_COMMAND.RVS_HEARTBEATCOMMAND:
                case RVS_COMMAND.RVS_HEARTCOMMAND:
                case RVS_COMMAND.RVS_STATUSREADCOMMAND:
                case RVS_COMMAND.RVS_SAVEPARACOMMAND:
                case RVS_COMMAND.RVS_BROADCASTCOMMAND:
                case RVS_COMMAND.RVS_SAVERAWDATACOMMAND:
                case RVS_COMMAND.RVS_FCTADCTESTCOMMAND:
                case RVS_COMMAND.RVS_INSTALLPARAMCOMMAND:
                case RVS_COMMAND.RVS_SAMPLEPARAMCOMMAND:
                case RVS_COMMAND.RVS_ALGORITHMCOMMAND:
                case RVS_COMMAND.RVS_SYSTEMTIMECOMMAND:
                case RVS_COMMAND.RVS_SOFTFUNCCOMMAND:
                case RVS_COMMAND.RVS_ANSWERCOMMAND:
                    if (null == pData.paramObject)
                    {
                        if (0 != pData.paramLen)
                        {
                            //Console.WriteLine("NULL pointer rvs_FormRVSPacket  pData.param \r\n");
                            return (int)RVS_ERROR.RVS_ERR_OTHER;
                        }
                    }
                    else
                    {
                        //memcpy(&answer[7], pData.param, pData.paramLen);
                        IntPtr paramBuf = Marshal.AllocHGlobal(Marshal.SizeOf(pData.paramObject));
                        if (paramBuf != IntPtr.Zero)
                        {
                            Marshal.StructureToPtr(pData.paramObject, paramBuf, false);
                            Marshal.Copy(paramBuf, answer, 7, pData.paramLen);
                            Marshal.FreeHGlobal(paramBuf);
                        }
                    }

                    tempLen = 7 + pData.paramLen;
                    break;

                default:
                    Console.WriteLine("this command not support \r\n");
                    return (int)RVS_ERROR.RVS_ERR_COMMADN;
            }

            answer[tempLen] = rvs_getChecksum(answer, tempLen + 1); // 校验和不包括帧起始码
            tempLen++; //  总长度
            if (tempLen <= pBuflen)
            {
                Array.Copy(answer, pBuf, tempLen);
                return tempLen;
            }
            else
            {
                return (int)RVS_ERROR.RVS_ERR_OTHER;
            }
        }

        static byte rvs_getChecksum(byte[] array, int packetLen)
        {
            byte sum = 0;
            packetLen--; //减去校验位
            for (int i = FRAMEHEADLEN; i < packetLen; i++)
            {
                sum += array[i];
            }

            return sum;
        }

        public static RVS_ERROR rvs_GetStatusPacket(ref rvs_PARAM_STATUS status, ref rvs_FRAME_Context context)
        {
            object sp_Status = BytesToStructure(context.paramBytes, typeof(rvs_PARAM_STATUS));
            if (null == sp_Status)
            {
                return RVS_ERROR.RVS_ERR_OTHER;
            }
            else
            {
                status = (rvs_PARAM_STATUS)sp_Status;
                return RVS_ERROR.RVS_OK;
            }
        }

        public static RVS_ERROR rvs_UnpackCommand(ref rvs_FRAME_Context pst_proto,
            byte[] pBuf, ref int len)
        {
            if (len < 20 && len > 4 && pBuf[4] == 0xA9)
            {
                return RVS_ERROR.RVS_ERR_OTHER;
            }
            if (len < 20 && len > 4 && pBuf[4] == 0xA8)
            {
                return RVS_ERROR.RVS_ERR_OTHER;
            }

            if (pBuf.Length > len || 0 == pBuf.Length)
            {
                //Console.WriteLine("NULL pointer UnpackData\r\n");
                return RVS_ERROR.RVS_ERR_OTHER;
            }

            RVS_ERROR err = rvs_checkIsCompleteFrame(pBuf, ref len);
            if (err != RVS_ERROR.RVS_OK)
            {
                //Console.WriteLine("is not a CompleteFrame  \r\n");
                return err;
            }
            else
            {
                pst_proto.srcAddr = (RVS_DeviceAddress)pBuf[2];
                pst_proto.dstAddr = (RVS_DeviceAddress)pBuf[3];
                //

                pst_proto.command = (RVS_COMMAND)pBuf[4];
                pst_proto.paramBytes = null;
                ushort paramLen = (ushort)(pBuf[5] + (pBuf[6] << 8));
                if (pst_proto.command == RVS_COMMAND.RVS_ANSWERCOMMAND) // 确认帧
                {
                    paramLen--;
                    pst_proto.command = (RVS_COMMAND)pBuf[7]; // 发送命令码
                    pst_proto.paramLen = paramLen; // 去掉确认命令0XA2的长度

                    if (pst_proto.paramLen > 0)
                    {
                        byte[] paramBytes = new byte[paramLen];
                        Array.Copy(pBuf, 8, paramBytes, 0, paramBytes.Length);
                        pst_proto.paramBytes = paramBytes;
                    }
                }
                else
                {
                    pst_proto.paramLen = paramLen;

                    if (pst_proto.paramLen > 0)
                    {
                        byte[] paramBytes = new byte[paramLen];
                        Array.Copy(pBuf, 7, paramBytes, 0, paramBytes.Length);
                        pst_proto.paramBytes = paramBytes;
                    }
                }
                // pst_proto.typenew = (RVS_DeviceAddressNEW)pBuf[17];
                return RVS_ERROR.RVS_OK;
            }
        }

        public static RVS_ERROR rvs_checkIsCompleteFrame(byte[] pBuf, ref int len)
        {
            RVS_ERROR ret = RVS_ERROR.RVS_OK;
            if (null == pBuf || pBuf.Length < len || pBuf.Length < FRAMELNETOPARAMCONTENT)
            {
                Console.WriteLine("NULL pointer findFrameHead\r\n");
                return RVS_ERROR.RVS_ERR_OTHER;
            }
            if (len < FRAMELNETOPARAMCONTENT)
            {
                return RVS_ERROR.RVS_ERR_LEN;
            }

            if (false == ((RVS_HEARD_0 == pBuf[0]) && (RVS_HEARD_1 == pBuf[1])))
            {
                Console.WriteLine("the frame is not a complete frame!!!");
                return RVS_ERROR.RVS_ERR_NOHEAD;
            }

            int paramLen = pBuf[6];
            paramLen = paramLen << 8;
            paramLen += pBuf[5];
            int bufLen = len;
            len = paramLen + FRAMELNETOPARAMCONTENT;
            if (len > bufLen)
            {
                return RVS_ERROR.RVS_ERR_LEN;
            }

            // 校验和不包括帧起始码和校验值
            byte checkSum = rvs_getChecksum(pBuf, len);
            if (checkSum == pBuf[len - CHECKLEN])
            {
                //Console.WriteLine(" checkSum is right\r\n");
                return RVS_ERROR.RVS_OK;
            }
            else
            {
                //Console.WriteLine(" checkSum is error the checkSum = %x \r\n", checkSum);
                return RVS_ERROR.RVS_ERR_CHECKCODE;
            }
        }
    }
}
