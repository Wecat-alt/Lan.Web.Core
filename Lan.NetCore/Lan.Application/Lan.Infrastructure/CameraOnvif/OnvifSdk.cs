using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Lan.Infrastructure.CameraOnvif
{
    #region 结构体
    //  public const int NET_DVR_GET_SHOWSTRING = 130;//获取叠加字符参数
    [StructLayout(LayoutKind.Sequential)]
    public struct ONVIF_PTZ_ABSOLUTEMOVE
    {
        public float panSpeed;     //水平速度 	[0, 1]
        public float tiltSpeed;    //垂直速度 	[0, 1]
        public float zoomSpeed;    //变倍速度  [0, 1]
        public float panPosition;  //水平位置  [-1, 1]
        public float tiltPosition;  //垂直位置 [-1, 1] 若只想绝对变倍，panPosition、tiltPosition可设置为-2.0;
        public float zoomPosition;   //变倍变数 [0, 1] 若只想水平垂直运动，zoomPosition可设置为-2.0; 
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct ONVIF_PTZ_CONTINUSMOVE
    {
        public float panSpeed;  	//水平运动速度 [-1, 1]
        public float tiltSpeed;  	//垂直运动速度 [-1, 1]
        public float zoomSpeed; 	//变倍速度 [-1, 1]
        public int timeout;		//0：表示一直运动，其他为运动毫秒
    };

    //[StructLayout(LayoutKind.Sequential)]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
    public struct ONVIF_DEVICE_PROBE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string ip;			//ip地址
        public int port;                        //ONVIF端口号
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] onvifUrl;    //ONVIF服务地址
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string devName;		//设备名
        public int devType;					//设备类型 0:IPC; 1:NVR;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string location;		//位置
    }

    [ComVisibleAttribute(true)]
    public enum ONVIF_CAPABILITIES
    {
        ONVIF_Mgmt_Capabilities_FALSE = 0,
        ONVIF_Mgmt_Capabilities_TRUE = 1
    };

    [ComVisibleAttribute(true)]
    public enum ONVIF_MEDIA_STREAM_TYPE
    {
        ONVIF_MEDIA_TYPE_MAIN_STREAM = 0,  //主码流
        ONVIF_MEDIA_TYPE_SUB_STREAM,	  //辅码流
        ONVIF_MEDIA_TYPE_THIRD_STREAM	 //三码流
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct ONVIF_MANAGEMENT_CAPABILITIES
    {
        public ONVIF_CAPABILITIES analytics;					//视频分析
        public ONVIF_CAPABILITIES device;						//设备管理
        public ONVIF_CAPABILITIES events;						//事件报警
        public ONVIF_CAPABILITIES imaging;					//图像
        public ONVIF_CAPABILITIES media;						//媒体配置
        public ONVIF_CAPABILITIES ptz;						//云台控制
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)] //注意：这里对应一维数组        
        public onvifUrlsByte[] onvifUrls; //ONVIF的各项服务地址
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct onvifUrlsByte
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] onvifUrls;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ONVIF_COMMON_INFO
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string username; //用户名
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string password; //密码
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public onvifUrlsByte[] onvifUrls;	//ONVIF的各项服务地址
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string sourceToken;
    };
    public struct ONVIF_FLOATRANGE
    {
        public float min;
        public float max;
    }
    ;

    public struct OnvifManage_COMMON_INFO
    {
        public string ip; //用户名
        public ONVIF_COMMON_INFO COMMON_INFO;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct ONVIF_DEVICE_INFO
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] firmwareVersion;	//固件版本
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] hardwareId;		//硬件ID
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] manufacturer;		//厂家
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] model;			//设备类型名
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] serialNumber;		//设备序列号
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct ONVIF_MEDIA_CHANNEL_SOURCE
    {
        public int sourceNum;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public sourceTokenByte[] sourceToken;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct sourceTokenByte
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] sourceToken;
    }

    public struct ONVIF_PTZ_GOTOPRESET
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] presetId; //预置点id
        public float panSpeed;  	//速度 [0, 1]
        public float tiltSpeed;
        public float zoomSpeed;
    };

    //预置点
    [StructLayout(LayoutKind.Sequential)]
    public struct ONVIF_PTZ_PRESETS
    {
        int sizePreset; //当前预置点数量
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public nameByte[] name; //预置点名
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public presetIdByte[] id; //预置点id
    };

    public struct nameByte
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] name;
    }
    public struct presetIdByte
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] presetId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ONVIF_PTZ_STATUS
    {
        public float panPosition;   	//云台当前水平位置 [-1, 1]
        public float tiltPosition;   	//云台当前垂直位置 [-1, 1]
        public float zoomPosition;  	//云台当前变倍位置 [0,  1]
        public ONVIF_MOVE_STATUS panTilt; //水平垂直运动状态
        public ONVIF_MOVE_STATUS zoom; //镜头变倍状态
    };

    [ComVisibleAttribute(true)]
    public enum ONVIF_MOVE_STATUS
    {
        ONVIF__MoveStatus__IDLE = 0,
        ONVIF__MoveStatus__MOVING = 1,
        ONVIF__MoveStatus__UNKNOWN = 2
    };
    #endregion



    public static class onvifsdk
    {
        //[DllImport("libeay32.dll", EntryPoint = "libeay32", CallingConvention = CallingConvention.Cdecl)]
        //public static extern bool libeay32();

        //[DllImport("msvcr120.dll", EntryPoint = "msvcr120", CallingConvention = CallingConvention.Cdecl)]
        //public static extern bool msvcr120();

        /// <summary>
        /// 获取在线设备
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="maxDevNum"></param>
        /// <param name="pDevice"></param>
        /// <param name="deviceNum"></param>
        /// <returns></returns>
        [DllImport("onvif_native", EntryPoint = "ONVIF_DISCOVERY_Probe", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_DISCOVERY_Probe(int timeout, int maxDevNum, IntPtr pDevice, int deviceNum);

        [DllImport("onvif_native", EntryPoint = "ONVIF_DISCOVERY_ProbeUnicast", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_DISCOVERY_ProbeUnicast(int timeout, string pDeviceIp, ref ONVIF_DEVICE_PROBE oNVIF_DEVICE_PROBE);  //单播探测

        /// <summary>
        /// 登陆onvif
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="capabilities"></param>
        /// <returns></returns>
        [DllImport("onvif_native", EntryPoint = "ONVIF_MAGEMENT_GetCapabilitiesEx", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_MAGEMENT_GetCapabilitiesEx(int timeout, string ip, int port, string username, string password, ref ONVIF_MANAGEMENT_CAPABILITIES capabilities); //获取能力集

        /// <summary>
        /// 获取设备信息
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="pCommon"></param>
        /// <param name="pDevice"></param>
        /// <returns></returns>
        [DllImport("onvif_native", EntryPoint = "ONVIF_MAGEMENT_GetDeviceInformation", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_MAGEMENT_GetDeviceInformation(int timeout, ref ONVIF_COMMON_INFO pCommon, ref ONVIF_DEVICE_INFO pDevice); //获取设备信息

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="pCommon"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        [DllImport("onvif_native", EntryPoint = "ONVIF_MEDIA_GetChannelSource", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_MEDIA_GetChannelSource(int timeout, ref ONVIF_COMMON_INFO pCommon, ref ONVIF_MEDIA_CHANNEL_SOURCE channel);

        /// <summary>
        /// 获取设备最大支持的码流数
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="pCommon"></param>
        /// <param name="sourceToken"></param>
        /// <param name="streamNum"></param>
        /// <returns></returns>
        [DllImport("onvif_native", EntryPoint = "ONVIF_MEDIA_GetStreamNum", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_MEDIA_GetStreamNum(int timeout, ref ONVIF_COMMON_INFO pCommon, string sourceToken, ref int streamNum);

        /// <summary>
        /// 获取rtsp地址
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="pCommon"></param>
        /// <param name="sourceToken"></param>
        /// <param name="streamType"></param>
        /// <param name="rtspUrl"></param>
        /// <returns></returns>
        [DllImport("onvif_native", EntryPoint = "ONVIF_MEDIA_GetStreamUri", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_MEDIA_GetStreamUri(int timeout, ref ONVIF_COMMON_INFO pCommon, byte[] rtspUrl);

        //[DllImport("ssleay32.dll", EntryPoint = "ssleay32", CallingConvention = CallingConvention.Cdecl)]
        //public static extern bool ssleay32();


        /// <summary>
        /// 连续运动
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="pCommon"></param>
        /// <param name="pContinusMove"></param>
        /// <returns></returns>
        [DllImport("onvif_native", EntryPoint = "ONVIF_PTZ_ContinusMove", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_PTZ_ContinusMove(int timeout, ref ONVIF_COMMON_INFO pCommon, ref ONVIF_PTZ_CONTINUSMOVE pContinusMove);

        /// <summary>
        /// 连续运行PTZ停止
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="pCommon"></param>
        /// <returns></returns>
        [DllImport("onvif_native", EntryPoint = "ONVIF_PTZ_ContinusStop", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_PTZ_ContinusStop(int timeout, ref ONVIF_COMMON_INFO pCommon);

        /// <summary>
        /// 绝对运动
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="pCommon"></param>
        /// <param name="pAbsoluteMove"></param>
        /// <returns></returns>
        [DllImport("onvif_native", EntryPoint = "ONVIF_PTZ_AbsoluteMove", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_PTZ_AbsoluteMove(int timeout, ref ONVIF_COMMON_INFO pCommon, ref ONVIF_PTZ_ABSOLUTEMOVE pAbsoluteMove);

        /// <summary>
        /// 运动到零位置
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="pCommon"></param>
        /// <returns></returns>
        [DllImport("onvif_native", EntryPoint = "ONVIF_PTZ_GotoHomePosition", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_PTZ_GotoHomePosition(int timeout, ref ONVIF_COMMON_INFO pCommon);  //


        /// <summary>
        /// 获取预置点列表
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="pCommon"></param>
        /// <param name="pPresets"></param>
        /// <returns></returns>
        [DllImport("onvif_native", EntryPoint = "ONVIF_PTZ_GetPresets", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_PTZ_GetPresets(int timeout, ref ONVIF_COMMON_INFO pCommon, IntPtr pPresets);			 //获取预置点列表
        /// <summary>
        /// 设置巡视点
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="pCommon"></param>
        /// <param name="presetName"></param>
        /// <param name="presetId"></param>
        /// <returns></returns>
        [DllImport("onvif_native", EntryPoint = "ONVIF_PTZ_SetPresets", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_PTZ_SetPresets(int timeout, ref ONVIF_COMMON_INFO pCommon, string presetName, byte[] presetId);		//设置预置点

        /// <summary>
        /// 修改预置点
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="pCommon"></param>
        /// <param name="presetName"></param>
        /// <param name="presetId"></param>
        /// <returns></returns>
        [DllImport(@"onvifSdk\x64\OnvifClient.dll", EntryPoint = "ONVIF_PTZ_ModifyPresets", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_PTZ_ModifyPresets(int timeout, ref ONVIF_COMMON_INFO pCommon, string presetName, string presetId);	//修改预置点

        /// <summary>
        /// 转到预置点
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="pCommon"></param>
        /// <param name="pGotoPreset"></param>
        /// <returns></returns>
        [DllImport("onvif_native", EntryPoint = "ONVIF_PTZ_GotoPreset", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_PTZ_GotoPreset(int timeout, ref ONVIF_COMMON_INFO pCommon, ONVIF_PTZ_GOTOPRESET pGotoPreset);      //转到预置点


        [DllImport("onvif_native", EntryPoint = "ONVIF_PTZ_RemovePreset", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_PTZ_RemovePreset(int timeout, ref ONVIF_COMMON_INFO pCommon, byte[] presetId);                       //删除预置点

        [DllImport("onvif_native", EntryPoint = "ONVIF_PTZ_GetStatus", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_PTZ_GetStatus(int timeout, ref ONVIF_COMMON_INFO pCommon, ref ONVIF_PTZ_STATUS ptzstatus);



        //=================================================================================================
        //===================================Image=========================================================
        //=================================================================================================
        [DllImport("onvif_native", EntryPoint = "ONVIF_IMAGING_GetMoveOptions", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ONVIF_IMAGING_GetMoveOptions(int timeout, ref ONVIF_COMMON_INFO pCommon, ref ONVIF_FLOATRANGE pRange);      //获取聚焦范围
    }
}
