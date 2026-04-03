using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Infrastructure.CameraOnvif
{
    //跟踪模式
    [ComVisibleAttribute(true)]
    public enum eTrackMode
    {
        eTrack_Distance_Priority = 0, //距离优先
        eTrack_Polling //轮询
    };

    //ONVIF值映射到角度结构体
    [StructLayout(LayoutKind.Sequential)]
    public struct ONVIF_ANGLE_MAP_T
    {
        public float fromOnvifVal;  //从ONVIF值
        public float toOnvifVal;   //到ONVIF值

        public float fromAngle;	//从球机角度
        public float toAngle;    //到球机角度
    };


    //球机标定与跟踪参数
    [StructLayout(LayoutKind.Sequential)]
    public struct BALL_PARA_T
    {
        public int trackMode;			//0:连续运动；1:绝对运动
        //public float minViewAngle;		//最小视场角度  测出来
        //public float maxViewAngle;		//最大视场角度  测出来
        public float viewAngle1X;		//1倍视场角  ..最大倍数 1
        public float viewAngle2X;		//2倍视场角 33
        public float viewAngleMaxX;      //最大倍数视场角 22

        public int maxZoom;			//球机最大倍数      多少倍球机
        public float maxZoomRatio;		//最大变倍比例系数（为-1会启用默认参数）  -1即可
        public int panCounterclockwise; //水平ONVIF[-1,1]增大是否逆时针旋转 0:否；1:是   
        public ONVIF_ANGLE_MAP_T panLeftToMiddle;	//水平方向从左到中间ONVIF[-1,0]对应球机             0---180---360  
        public ONVIF_ANGLE_MAP_T panMiddleToRight;	//水平方向从中间到右边ONVIF[0,1]对应球机角度        0---180---360  
        public ONVIF_ANGLE_MAP_T tiltTopToBottom;	//垂直方向从上往下ONVIF值对应球机角度

        public float panKp;				//水平比例系数      连续运动使用
        public float panKi;				//水平积分系数      连续运动使用
        public float tiltKp;			//垂直比例系数  连续运动使用
        public float tiltKi;			//垂直积分系数  连续运动使用

        //public float panOffsetAngle;		//球机相对于雷达零位水平偏移量(角度)
        //public float installHeight;		//球机安装高度(实际安装高度基础上减去0.5米)

        public float ballPointX;			//球机点坐标X(单位:米)
        public float ballPointY;			//球机点坐标Y(单位:米)
        public float referPointX;		//参照点坐标X(单位:米)
        public float referPointY;		//参照点坐标Y(单位:米)
        public float referPointPan;		//球机转动到参照点时当前ONVIF Pan值[-1,1]
        public float installHeight;		//球机安装高度(实际安装高度基础上减去0.5米, 单位:米)

        //中心偏移量标定
        public float minZoomPan;         //最小倍数下调整球机中心转到参照点A，当前ONVIF Pan、Tilt
        public float minZoomTilt;
        public float maxZoomPan;		    //最大倍数下调整球机中心转到参照点A，当前ONVIF Pan、Tilt
        public float maxZoomTilt;

    };

    //雷达目标数据
    [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct RADAR_TARGETS_T
    {
        public int targetId;                     //目标ID范围0x01~0x40
        public uint type;                //目标类型
        public float speed_X;                      //X方向速度
        public float speed_Y;                      //Y方向速度
        public float cod_X;                        //X方向坐标
        public float cod_Y;                        //Y方向坐标
        public float distance;                     //目标距离
        public float azimuth;                      //目标方位角
    };

    public static class RBTrackSdk
    {

        //当前目标跟踪回调函数
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int TrackCallBack(int channelId, ref RADAR_TARGETS_T target, IntPtr userPtr);

        /**
        * @brief	初始化
        * @param	maxChannel	支持最大通道数	
        * @return	0:成功；其他:失败   	
        */
        [DllImport("rbtrack_native", EntryPoint = "RBTRACK_Init", CallingConvention = CallingConvention.Cdecl)]
        public static extern int RBTRACK_Init(int maxChannel = 256);

        /**
         * @brief	创建雷球跟踪
         * @param	ip			球机IP
         * @param	username	球机用户名
         * @param	password	球机密码
         * @param	cameraId	相机id,默认从0开始
         * @param	ballPara	球机标定和跟踪参数
         * @param	callback	跟踪目标回调函数
         * @param	userPtr		用户指针
         * @return	channelId	跟踪通道ID；<0:失败
         */
        [DllImport("rbtrack_native", EntryPoint = "RBTRACK_CreateTrack", CallingConvention = CallingConvention.Cdecl)]
        public static extern int RBTRACK_CreateTrack(string ip, string username, string password, int cameraId, ref BALL_PARA_T ballPara, TrackCallBack callback, IntPtr userPtr);

        /**
        * @brief	设置跟踪模式
        * @param	channelId	通道ID
        * @param	trackMode	跟踪模式，默认距离优先
        * @return	0:成功；其他:失败
        */
        [DllImport("rbtrack_native", EntryPoint = "RBTRACK_SetTrackMode", CallingConvention = CallingConvention.Cdecl)]
        public static extern int RBTRACK_SetTrackMode(int channelId, eTrackMode trackMode);

        /**
        * @brief	更新雷达目标数据
        * @param	channelId	通道ID
        * @param	targets		雷达目标数据
        * @param	targetNum	目标数组大小
        * @return	0:成功；其他:失败
        */
        //[DllImport("rbtrack_native", EntryPoint = "RBTRACK_UpdateTargets", CallingConvention = CallingConvention.Cdecl)]
        //public static extern int RBTRACK_UpdateTargets(int channelId, IntPtr targets, int targetNum);
        [DllImport("rbtrack_native", EntryPoint = "RBTRACK_UpdateTargets", CallingConvention = CallingConvention.Cdecl)]
        public static extern int RBTRACK_UpdateTargets(int channelId, RADAR_TARGETS_T[] targets, int targetNum);

        /**
        * @brief	删除雷球跟踪
        * @param	channelId	通道ID
        * @return	0:成功；其他:失败
        */
        [DllImport("rbtrack_native", EntryPoint = "RBTRACK_DeleteTrack", CallingConvention = CallingConvention.Cdecl)]
        public static extern int RBTRACK_DeleteTrack(int channelId);

        /**
        * @brief	反初始化
        * @param	无
        * @return	0:成功；其他:失败
        */
        [DllImport("rbtrack_native", EntryPoint = "RBTRACK_DeInit", CallingConvention = CallingConvention.Cdecl)]
        public static extern int RBTRACK_DeInit();
    }
}
