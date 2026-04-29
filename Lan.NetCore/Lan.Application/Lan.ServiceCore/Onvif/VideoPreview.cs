using Infrastructure;
using Lan.Infrastructure;
using Lan.Infrastructure.CameraOnvif;
using Lan.ServiceCore.TargetCollection;
using MemoryCache.Core;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lan.ServiceCore.Onvif
{
    public class VideoPreview
    {
        #region API声明
        public const int MEDIA_TYPE_VIDEO = 0x00000001;
        private const int MEDIA_TYPE_EVENT = 0x00000004;
        private IntPtr wndHeader;//控件窗口句柄，用于防止异步操作时获取句柄失败
        public const int MAX_CHANNEL_NUM = 128;
        private object _closeLockObj = new object();

        public enum LIVE_CHANNEL_SOURCE_TYPE_ENUM
        {
            /// <summary>
            ///  //源为RTSP(内置)
            /// </summary>
            LIVE_CHANNEL_SOURCE_TYPE_RTSP = 0, //源为RTSP(内置)

            /// <summary>
            /// 源为RTMP(内置)
            /// </summary>
            LIVE_CHANNEL_SOURCE_TYPE_RTMP, //源为RTMP(内置)

            /// <summary>
            /// 源为HLS(内置)
            /// </summary>
            LIVE_CHANNEL_SOURCE_TYPE_HLS, //源为HLS(内置)

            /// <summary>
            /// 源为本地文件(内置)
            /// </summary>
            LIVE_CHANNEL_SOURCE_TYPE_FILE, //源为本地文件(内置)

            /// <summary>
            /// 源为外部的编码数据
            /// </summary>
            LIVE_CHANNEL_SOURCE_TYPE_ENCODE_DATA, //源为外部的编码数据

            /// <summary>
            /// 源为外部的解码数据
            /// </summary>
            LIVE_CHANNEL_SOURCE_TYPE_DECODE_DATA, //源为外部的解码数据
        }

        public enum LIVE_CALLBACK_TYPE_ENUM
        {
            LIVE_TYPE_CONNECTING = 100, //当前通道连接中
            LIVE_TYPE_CONNECTED, //当前通道已连接
            LIVE_TYPE_RECONNECT, //当前通道连接已断开,正在重连
            LIVE_TYPE_DISCONNECT, //当前通道连接已中止(内部连接线程已退出),指定了连接次数的情况下会回调该值

            LIVE_TYPE_CODEC_DATA, //编码数据
            LIVE_TYPE_DECODE_DATA, //解码数据
            LIVE_TYPE_SNAPSHOT, //抓拍
            LIVE_TYPE_RECORDING, //录像
            LIVE_TYPE_PLAYBACK_TIME, //当前回放时间
            LIVE_TYPE_METADATA,
            LIVE_TYPE_DISPLAY,//绘图回调

            LIVE_TYPE_START_PLAY_AUDIO, //开始播放声音
            LIVE_TYPE_STOP_PLAY_AUDIO, //停止播放声音
            LIVE_TYPE_CAPTURE_AUDIO_DATA, //本地采集的音频数据

            LIVE_TYPE_FILE_DURATION, //文件时长

            LIVE_TYPE_RECORDING_PLAYBACK_COMPLETE, //录像回放完成	 当为该类型时,回调中的channelId为回放通道ID,可根据userPtr进行处理, mediaType为MEDIA_TYPE_EVENT, pbuf为NULL, frameInfo为NULL
        }

        public enum RENDER_FORMAT
        {
            RENDER_FORMAT_YV12 = 842094169,
            RENDER_FORMAT_YUY2 = 844715353,
            RENDER_FORMAT_UYVY = 1498831189,
            RENDER_FORMAT_A8R8G8B8 = 21,
            RENDER_FORMAT_X8R8G8B8 = 22,
            RENDER_FORMAT_RGB565 = 23,
            RENDER_FORMAT_RGB555 = 25
        }

        /// <summary>
        /// 播放速度
        /// </summary>
        public enum PLAY_SPEED_ENUM
        {
            PLAY_SPEED_UNKNOWN = -1,
            PLAY_SPEED_NORMAL = 0x00, // 正常播放
            PLAY_SPEED_PAUSED, // 暂停
            PLAY_SPEED_SLOW_X2, // 1/2
            PLAY_SPEED_SLOW_X4, // 1/4
            PLAY_SPEED_SLOW_X8, // 1/8
            PLAY_SPEED_SLOW_X16, // 1/16
            PLAY_SPEED_FAST_X2, // x2
            PLAY_SPEED_FAST_X4, // x4
            PLAY_SPEED_FAST_X8, // x8
            PLAY_SPEED_FAST_X16, // x16
            PLAY_SPEED_REWIND_X2, // -2x
            PLAY_SPEED_REWIND_X4, // -4x
            PLAY_SPEED_REWIND_X8, // -8x	
            PLAY_SPEED_REWIND_X16, // -16x
            PLAY_SPEED_SINGLE_FRAME, //单帧播放,手动单击
        }

        /// <summary>
        /// 帧信息
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct LIVE_FRAME_INFO
        {
            public int codec; //编码格式
            public byte type; //帧类型
            public byte fps; //帧率
            public byte reserved1;
            public byte reserved2;

            public ushort width; //宽
            public ushort height; //高
            public int sample_rate; //采样率
            public int channels; //声道
            public int bitsPerSample; //采样精度
            public int length; //帧大小
            public int rtptimestamp_sec; //rtp timestamp	sec
            public int rtptimestamp_usec; //rtp timestamp	usec
            public int timestamp_sec; //秒

            public float bitrate; //Kbps
            public float losspacket;
        }

        /// <summary>
        /// 媒体信息
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct LIVE_MEDIA_INFO_T
        {
            uint videoCodec; //视频编码类型
            uint videoFps; //视频帧率
            int videoWidth;
            int videoHeight;
            float videoBitrate;

            uint audioCodec; //音频编码类型
            uint audioSampleRate; //音频采样率
            uint audioChannel; //音频通道数
            uint audioBitsPerSample; //音频采样精度

            uint metadataCodec; //Metadata类型

            uint vpsLength; //视频vps帧长度
            uint spsLength; //视频sps帧长度
            uint ppsLength; //视频pps帧长度
            uint seiLength; //视频sei帧长度

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
            byte[] vps; //视频vps帧内容

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
            byte[] sps; //视频sps帧内容

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            byte[] pps; //视频sps帧内容

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            byte[] sei; //视频sei帧内容
        }




        static VideoPreview()
        {
            try
            {
                NativeLibrary.SetDllImportResolver(typeof(VideoPreview).Assembly, new DllImportResolver(VideoPreview.ResolveNativeLibrary));
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static IntPtr ResolveNativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (!string.Equals(libraryName, "NovaPlayer", StringComparison.Ordinal))
            {
                return IntPtr.Zero;
            }
            if (OperatingSystem.IsWindows())
            {
                string text = Path.Combine(AppContext.BaseDirectory, "NovaPlayer", "x64", "NovaPlayer.dll");
                IntPtr result;
                if (File.Exists(text) && NativeLibrary.TryLoad(text, out result))
                {
                    return result;
                }
            }
            if (OperatingSystem.IsLinux())
            {
                string text = Path.Combine(AppContext.BaseDirectory, "NovaPlayer", "lib", "libNovaPlayer.so");
                IntPtr result2;
                if (File.Exists(text) && NativeLibrary.TryLoad(text, out result2))
                {
                    return result2;
                }
                if (NativeLibrary.TryLoad("libNovaPlayer.so", assembly, searchPath, out result2))
                {
                    return result2;
                }
            }
            IntPtr result3;
            if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out result3))
            {
                return result3;
            }
            throw new DllNotFoundException("无法加载原生库: " + libraryName);
        }


        public delegate int LivePlayerCallBack(LIVE_CALLBACK_TYPE_ENUM callbackType, int channelId, IntPtr userPtr,
            int mediaType, IntPtr pbuf, ref LIVE_FRAME_INFO frameInfo);


        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="supportMaxChannel">按需指定最大通道数, 最大不能超过宏定义<seealso cref="MAX_CHANNEL_NUM"/></param>
        /// <returns></returns>
        //[DllImport(DLL_NAME, CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
        //    CallingConvention = CallingConvention.Cdecl)]
        [DllImport("NovaPlayer", EntryPoint = "NovaPlayer_Initialize", CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_Initialize(int supportMaxChannel);

        /// <summary>
        /// 反初始化
        /// </summary>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_Deinitialize();

        /// <summary>
        /// 打开流, 返回一个channelId, 后续所有操作，都基于该channelId
        /// </summary>
        /// <param name="channelType">通道源类型</param>
        /// <param name="url"></param>
        /// <param name="rtpOverTcp">1为tcp, 0为udp</param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="callback"></param>
        /// <param name="userPtr">回调函数及自定义指针</param>
        /// <param name="reconnection">1表示无限次重连,0表示不重连,大于1表示指定次数重连(需小于1000)</param>
        /// <param name="connTimeout">连接超时</param>
        /// <param name="heartbeatType">0</param>
        /// <param name="queueSize">缓冲队列大小,需大于1024*512</param>
        /// <param name="multiplex">0x01:复用源,即打开同一个url时，到前端的连接只有一个  0x00:打开多少个url,就有多少个连接</param>
        /// <returns></returns>
        [DllImport("NovaPlayer", EntryPoint = "NovaPlayer_OpenStream", CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_OpenStream(LIVE_CHANNEL_SOURCE_TYPE_ENUM channelType,
            string url, byte rtpOverTcp,
            string username, string password,
            LivePlayerCallBack callback, IntPtr userPtr,
            int reconnection,
            int connTimeout,
            int heartbeatType,
            int queueSize,
            byte multiplex);

        /// <summary>
        /// 关闭流
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_CloseStream(int channelId);

        /// <summary>
        /// 开始播放     对同一个流,最大播放个数不能大于5个, 如果只调用 NovaPlayer_OpenStream 而不调用 NovaPlayer_StartPlayStream 则无此限制
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <param name="hWnd"></param>
        /// <param name="renderFormat"></param>
        /// <param name="decodeType">0:软解 1:硬解</param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_StartPlayStream(int channelId, IntPtr hWnd, RENDER_FORMAT renderFormat,
            byte decodeType = 0);

        /// <summary>
        /// 送编码流到指定通道, 需配合NovaPlayer_OpenStream中的channelType==LIVE_CHANNEL_SOURCE_TYPE_ENCODE_DATA
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <param name="mediaType"></param>
        /// <param name="frameInfo"></param>
        /// <param name="pbuf"></param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_PutFrameData(int channelId, int mediaType, ref LIVE_FRAME_INFO frameInfo,
            IntPtr pbuf);

        /// <summary>
        /// 清空帧队列, 将从下一个收到的关键帧开始播放
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_ResetFrameQueue(int channelId);

        /// <summary>
        /// 停止播放
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_StopPlayStream(int channelId);

        /// <summary>
        /// 获取指定通道的媒体信息
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <param name="pMediaInfo"></param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_GetStreamInfo(int channelId, ref LIVE_MEDIA_INFO_T pMediaInfo);

        /// <summary>
        /// 设置播放帧缓存, 1 - 10   帧数越小表示延时越小
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <param name="cache">缓存大小  1 - 10</param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_SetPlayFrameCache(int channelId, int cache);

        /// <summary>
        /// 设置中心十字架是否显示
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_SetCenterCross(int channelId, int visible);

        /// <summary>
        /// 获取播放帧缓存
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_GetPlayFrameCache(int channelId);

        /// <summary>
        /// 显示统计信息
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <param name="show"></param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_ShowStatisticalInfo(int channelId, byte show);

        /// <summary>
        /// 按比例显示
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <param name="scaleDisplay"></param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_SetScaleDisplay(int channelId, byte scaleDisplay);

        /// <summary>
        /// 设置播放速度
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <param name="speed"></param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_SetPlaySpeed(int channelId, PLAY_SPEED_ENUM speed);

        /// <summary>
        /// 获取当前播放速度
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern PLAY_SPEED_ENUM NovaPlayer_GetPlaySpeed(int channelId);

        /// <summary>
        /// 单帧播放, 可调用NovaPlayer_SetPlaySpeed切换回正常播放模式
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_PlaySingleFrame(int channelId);

        /// <summary>
        /// 跳转到指定时间播放
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <param name="playTime">YYYYMMDDTHHMMSSZ  例: 20170208T092700Z</param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_ChangePlayTime(int channelId, string playTime);

        /// <summary>
        /// 跳转到指定时间播放(文件)
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <param name="playTimeSecs">秒</param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_SeekFile(int channelId, uint playTimeSecs);

        /// <summary>
        /// 抓图到文件, 只有在异步抓图模式下，才能使用队列,队列内存为31104000bytes(大约为32MB)
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <param name="imageFormat">0:bmp 1:jpg</param>
        /// <param name="filename"></param>
        /// <param name="sync">0:异步: 1:同步</param>
        /// <param name="useQueue">1:使用队列 0:不使用队列</param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_SnapshotToFile(int channelId,
            byte imageFormat,
            string filename,
            byte sync = 0,
            byte useQueue = 0);

        /// <summary>
        /// 抓图到内存  现仅支持RGB, 内存空间需调用者分配
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <param name="imageFormat"></param>
        /// <param name="out_imageData"></param>
        /// <param name="out_imageSize"></param>
        /// <param name="out_width"></param>
        /// <param name="out_height"></param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_SnapshotToMemory(int channelId,
            byte imageFormat,
            IntPtr out_imageData,
            out int out_imageSize,
            out int out_width,
            out int out_height);
        /// <summary>
        /// 开始录像，返回值大于等于0表成功，小于0表失败
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <param name="foldername">存储目录</param>
        /// <param name="filename">存储文件名</param>
        /// <param name="filesize">录像文件大小，现阶段无效</param>
        /// <param name="duration">录像时长，现阶段无效</param>
        /// <param name="preRecording">0x01:预录  0x00:不预录；现阶段无效</param>
        /// <returns>返回值大于等于0表成功，小于0表失败</returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_StartRecording(int channelId, string foldername, string filename/*.mp4*/,
            uint filesize = 1024/*录像文件大小*/, int duration = 10/*录像时长*/, byte preRecording = 0/*0x01:预录  0x00:不预录*/);
        /// <summary>
        /// 停止录像
        /// </summary>
        /// <param name="channelId">播放名柄，<see cref="NovaPlayer_OpenStream"/>的返回值</param>
        /// <returns></returns>
        [DllImport("NovaPlayer", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int NovaPlayer_StopRecording(int channelId);



        // public bool SDK_REULT(int reult) => reult == 0;


        #endregion

        private readonly IMemoryCacheService? _cache;

        WCamera cameraBuffer; //相机缓冲区
        IntPtr handle; //视频播放句柄
        ONVIF_COMMON_INFO common = new ONVIF_COMMON_INFO();
        public string visible = "0";

        private LivePlayerCallBack _livePlayerCallBack;
        private int _playId;
        private bool IsRecord = false; //是否录像                                                                    

        string Recordfilename = "";

        public VideoPreview()
        {
            
            _cache = App.GetService<IMemoryCacheService>();
            //this.handle = this.Handle; //视频句柄
            //LoadPath("NovaPlayer");        //加载onvif DLL路径
            Console.WriteLine("Initializing NovaPlayer...");
            int ret = VideoPreview.NovaPlayer_Initialize(MAX_CHANNEL_NUM);
            Console.WriteLine("NovaPlayer initialization result: " + ret);
            DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(22, 1);
            defaultInterpolatedStringHandler.AppendLiteral("NovaPlayer_Initialize:");
            defaultInterpolatedStringHandler.AppendFormatted<int>(ret);
            _livePlayerCallBack = LivePlayerCallback;
        }


        public static bool LoadPath(string folder)
        {
            try
            {
                //库文件路径
                string libPath = System.IO.Path.Combine(Environment.CurrentDirectory, folder);

                //根据软件运行模式设置库文件的路径
                if (Environment.Is64BitProcess)
                {
                    libPath = System.IO.Path.Combine(libPath, "X64");
                }
                else
                {
                    libPath = System.IO.Path.Combine(libPath, "X86");
                }

                //获取当前环境变量Path的值
                var path = Environment.GetEnvironmentVariable("Path");
                //添加库路径到环境变量中
                path += (";" + libPath + ";");
                //设置环境变量
                Environment.SetEnvironmentVariable("Path", path);

                // LogProc.Log.Debug("添加了环境变量" + libPath);

                return true;
            }
            catch (Exception ex)
            {
                //LogProc.Log.Error(ex.ToString());
                return false;
            }
        }


        public int PreviewReconrd(WCamera f_cameraBuffer, string svisible = "0", WDefenceArea defenceToBind = null)
        {
            visible = svisible;

            if (f_cameraBuffer == null)
            {
                //defence = null;
                return -1;
            }
            else
            {
                this.cameraBuffer = f_cameraBuffer;

                //common = MemoryCacheHelper.Get<ONVIF_COMMON_INFO>(f_cameraBuffer.Ip);
                common = _cache.Get<ONVIF_COMMON_INFO>(f_cameraBuffer.Ip);

                int id = LoginRecord(f_cameraBuffer.Ip, f_cameraBuffer.Username, f_cameraBuffer.Password);
                return id;
            }
        }

        public int LoginRecord(string _ip, string _username, string _password)
        {
            try
            {
                //if (IsOpened)
                //    Close();

                int id = OpenRecord(cameraBuffer.cameraURL, _username, _password);
                return id;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        public int OpenRecord(string url, string user = null, string pwd = null, bool useTcp = true)
        {
            //if (IsOpened)
            //    Close();


            LIVE_CHANNEL_SOURCE_TYPE_ENUM type;

            type = LIVE_CHANNEL_SOURCE_TYPE_ENUM.LIVE_CHANNEL_SOURCE_TYPE_RTSP;


            int channel;
            lock (_closeLockObj)
            {
                channel = NovaPlayer_OpenStream(type, url, (byte)(useTcp ? 1 : 0), user, pwd, _livePlayerCallBack,
                    IntPtr.Zero, 1, 5, 0, 1024 * 1024, 1);
            }

            if (channel > 0)
            {
                _playId = channel;
                //_status = EStatus.Loaded;
                return _playId;
            }
            else
                return -1;

        }

        private int LivePlayerCallback(LIVE_CALLBACK_TYPE_ENUM callbackType, int channelId, IntPtr userPtr,
         int mediaType, IntPtr pbuf, ref LIVE_FRAME_INFO frameInfo)
        {
            try
            {


                //if (callbackType == LIVE_CALLBACK_TYPE_ENUM.LIVE_TYPE_RECORDING_PLAYBACK_COMPLETE && mediaType == MEDIA_TYPE_EVENT) //文件回放完成
                //{
                //    this.BeginInvoke(new Action(delegate
                //    {
                //        Close();
                //        Thread.Sleep(1000);
                //        PreviewRecord(cameraBuffer, Recordfilename);
                //    }
                //    ));
                //}
            }
            catch
            {
            }


            return 0;
        }

        public void BeginLocalRecord(string filename, WCamera Camera, int prplayid)
        {
            if (IsRecord)
                return;

            if (string.IsNullOrEmpty(filename))
                return;

            string[] str = filename.Split('/');
            string strfilename = str[str.Length - 1];

            string VideosStorePath = filename.Substring(0, filename.Length - strfilename.Length - 1);
            int ret = 0;
            lock (_closeLockObj)
            {
                ret = NovaPlayer_StartRecording(prplayid, VideosStorePath, strfilename);
            }

            if (ret >= 0)
            {
                //Log.Debug(prplayid.ToString() + "开始录像");

                IsRecord = true;
            }
        }
        /// <summary>
        /// 停止本地录像
        /// </summary>
        public void EndLocalRecord(int prplayid)
        {
            int ret = 0;
            lock (_closeLockObj)
            {
                ret = NovaPlayer_StopRecording(prplayid);
                IsRecord = false;
            }
            // Log.Debug(prplayid.ToString() + "停止录像");
        }
    }
}
