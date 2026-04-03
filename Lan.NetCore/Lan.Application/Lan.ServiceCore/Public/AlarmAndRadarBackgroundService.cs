using Dm;
using Dm.util;
using Lan.Infrastructure;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.Services;
using Lan.ServiceCore.TargetCollection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Model;
using SqlSugar;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Lan.ServiceCore.Public
{
    public class AlarmAndRadarBackgroundService : BackgroundService
    {
        private readonly ILogger<AlarmAndRadarBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        // 报警队列
        private readonly BlockingCollection<AlarmEvent> _alarmQueue = new();

        // 雷达数据通道
        private readonly Channel<TrackInfo> _radarChannel;
        private const int CHANNEL_CAPACITY = 10000;
        private const int BATCH_SIZE = 200;

        // 活跃报警字典
        private readonly ConcurrentDictionary<string, ActiveAlarmInfo> _activeAlarms = new();
        private readonly ConcurrentDictionary<string, VideoRecordingInfo> _videoRecordings = new();
        private readonly int _recordIntervalSeconds = 15; // 默认15秒，也是录像间隔

        // 服务实例
        private AlarmService _alarmService = new AlarmService();
        private TrackInfoService _trackInfoService = new TrackInfoService();

        private static AlarmAndRadarBackgroundService _instance;
        public static AlarmAndRadarBackgroundService Instance => _instance;

        // 内部类，用于跟踪活跃报警的信息
        private class ActiveAlarmInfo
        {
            public string ZoneId { get; set; }
            public string RadarIp { get; set; }
            public int CurrentAlarmId { get; set; } // 当前报警记录ID
            public DateTime StartTime { get; set; }
            public DateTime LastGeneratedRecordTime { get; set; } // 上次生成记录的时间
            public DateTime LastDataTime { get; set; } // 最后收到数据的时间
            public string CurrentVideoName { get; set; } // 当前录像文件名
            public bool IsFirstRecordGenerated { get; set; } = false; // 标记第一条记录是否已生成
            public List<TrackInfo> PendingTrackInfos { get; set; } = new List<TrackInfo>(); // 待处理的雷达数据
        }

        // 内部类，用于跟踪录像信息
        private class VideoRecordingInfo
        {
            public string ZoneId { get; set; }
            public string RadarIp { get; set; }
            public DateTime LastVideoStartTime { get; set; } // 上次开始录像的时间
            public bool IsRecording { get; set; } // 是否正在录像
        }

        public AlarmAndRadarBackgroundService(ILogger<AlarmAndRadarBackgroundService> logger, IServiceProvider serviceProvider)
        {
            _instance = this; // 设置静态实例
            _logger = logger;
            _serviceProvider = serviceProvider;

            // 初始化雷达数据通道
            var options = new BoundedChannelOptions(CHANNEL_CAPACITY)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };
            _radarChannel = Channel.CreateBounded<TrackInfo>(options);
        }

        // 报警事件写入方法
        public void WriteAlarm(AlarmEvent alarmEvent)
        {
            _alarmQueue.Add(alarmEvent);
        }

        // 雷达数据写入方法
        //public async ValueTask WriteRadarDataAsync(TrackInfo trackInfo, CancellationToken cancellationToken = default)
        //{
        //    await _radarChannel.Writer.WriteAsync(trackInfo, cancellationToken);
        //}

        public void WriteRadarData(TrackInfo trackInfo)
        {
            if (!_radarChannel.Writer.TryWrite(trackInfo))
            {
                _logger.LogWarning("通道已满，无法写入雷达数据，目标ID: {TargetId}", trackInfo.TargetId);
            }
        }

        /// <summary>
        /// 结束指定防区的报警
        /// </summary>
        public async Task EndAlarmAsync(string zoneId)
        {
            if (_activeAlarms.TryRemove(zoneId, out var alarmInfo))
            {
                // 处理待处理的雷达数据
                if (alarmInfo.PendingTrackInfos.Count > 0)
                {
                    await ProcessPendingTrackInfos(alarmInfo);
                }

                // 如果有录像，在生成最后一条记录前确保录像文件名已更新
                if (_videoRecordings.TryGetValue(zoneId, out var videoInfo) && videoInfo.IsRecording)
                {
                    // 停止录像并获取最终文件名
                    StopVideoRecording(zoneId);

                    // 如果有录像，确保录像文件名已更新到alarmInfo中
                    if (!string.IsNullOrEmpty(alarmInfo.CurrentVideoName))
                    {
                        // 生成一条结束记录
                        await GenerateAlarmRecord(zoneId, alarmInfo, true);
                    }
                }

                // 等待当前录像周期结束（如果有的话）
                await Task.Delay(100);

                // 停止录像并从字典中移除
                StopVideoRecording(zoneId);
                _videoRecordings.TryRemove(zoneId, out _);

                _logger.LogInformation($"报警结束: 防区 {zoneId}");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 启动三个并行任务
            var alarmProcessTask = Task.Run(() => ProcessAlarmQueueAsync(stoppingToken));
            var radarProcessTask = Task.Run(() => ProcessRadarChannelAsync(stoppingToken));
            var recordTask = Task.Run(() => GenerateAlarmRecordsAsync(stoppingToken));

            await Task.WhenAll(alarmProcessTask, radarProcessTask, recordTask);
        }

        /// <summary>
        /// 处理报警队列
        /// </summary>
        private async Task ProcessAlarmQueueAsync(CancellationToken cancellationToken)
        {
            foreach (var alarmEvent in _alarmQueue.GetConsumingEnumerable(cancellationToken))
            {
                try
                {
                    await ProcessSingleAlarmAsync(alarmEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"处理报警事件失败: {alarmEvent.ZoneId}");
                }
            }
        }

        /// <summary>
        /// 处理雷达数据通道
        /// </summary>
        private async Task ProcessRadarChannelAsync(CancellationToken cancellationToken)
        {
            var batch = new List<TrackInfo>(BATCH_SIZE);
            var timeout = TimeSpan.FromSeconds(10); // 超时时间10秒

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 等待数据，带超时
                    var hasData = await _radarChannel.Reader.WaitToReadAsync(cancellationToken).AsTask().WaitAsync(timeout, cancellationToken);

                    if (hasData)
                    {
                        // 有数据，读取所有可读的数据
                        while (_radarChannel.Reader.TryRead(out var data))
                        {
                            // 检查是否有对应的活跃报警
                            if (_activeAlarms.TryGetValue(data.AlarmId.ToString(), out var alarmInfo))
                            {
                                // 设置AlarmId
                                data.AlarmId = alarmInfo.CurrentAlarmId;
                                alarmInfo.LastDataTime = DateTime.Now;

                                // 添加到待处理列表
                                alarmInfo.PendingTrackInfos.Add(data);

                                // 如果待处理数据达到批量大小，立即处理
                                if (alarmInfo.PendingTrackInfos.Count >= BATCH_SIZE)
                                {
                                    await ProcessPendingTrackInfos(alarmInfo);
                                }
                            }
                            else
                            {
                                // 没有活跃报警，忽略这条数据或记录日志
                                _logger.LogDebug($"收到雷达数据但无活跃报警: 防区 {data.AlarmId}, 目标 {data.TargetId}");
                            }
                        }
                    }
                }
                catch (TimeoutException)
                {
                    // 超时，处理所有待处理的雷达数据
                    foreach (var alarmInfo in _activeAlarms.Values)
                    {
                        if (alarmInfo.PendingTrackInfos.Count > 0)
                        {
                            await ProcessPendingTrackInfos(alarmInfo);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理雷达数据通道时发生异常");
                }
            }

            // 服务停止时处理剩余数据
            foreach (var alarmInfo in _activeAlarms.Values)
            {
                if (alarmInfo.PendingTrackInfos.Count > 0)
                {
                    await ProcessPendingTrackInfos(alarmInfo);
                }
            }
        }

        /// <summary>
        /// 处理待处理的雷达数据
        /// </summary>
        private async Task ProcessPendingTrackInfos(ActiveAlarmInfo alarmInfo)
        {
            if (alarmInfo.PendingTrackInfos.Count == 0) return;

            try
            {
                var result = await _trackInfoService.BatchInsertAsync(alarmInfo.PendingTrackInfos);
                _logger.LogDebug($"成功批量插入 {alarmInfo.PendingTrackInfos.Count} 条雷达数据，关联AlarmId: {alarmInfo.CurrentAlarmId}");
                alarmInfo.PendingTrackInfos.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"批量插入雷达数据失败，关联AlarmId: {alarmInfo.CurrentAlarmId}");
            }
        }

        /// <summary>
        /// 处理单个报警事件（只更新状态，不生成记录）
        /// </summary>
        private async Task ProcessSingleAlarmAsync(AlarmEvent alarmEvent)
        {
            // 检查是否已有活跃报警
            if (!_activeAlarms.TryGetValue(alarmEvent.ZoneId, out var alarmInfo))
            {
                // 第一步：立即开始录像并获取录像文件名
                string videoName = StartVideoRecording(int.Parse(alarmEvent.ZoneId), alarmEvent.RadarIp);

                // 第二步：创建新的报警记录
                var newAlarmRecord = await CreateAlarmRecordAsync(alarmEvent.ZoneId, alarmEvent.RadarIp,
                    alarmEvent.AlarmTime, videoName, false);

                if (newAlarmRecord == null || newAlarmRecord.Id <= 0)
                {
                    _logger.LogError($"创建报警记录失败: 防区 {alarmEvent.ZoneId}");
                    return;
                }

                // 第三步：创建新的活跃报警信息
                var newAlarmInfo = new ActiveAlarmInfo
                {
                    ZoneId = alarmEvent.ZoneId,
                    RadarIp = alarmEvent.RadarIp,
                    CurrentAlarmId = newAlarmRecord.Id, // 保存生成的AlarmId
                    StartTime = alarmEvent.AlarmTime,
                    LastGeneratedRecordTime = alarmEvent.AlarmTime,
                    LastDataTime = alarmEvent.AlarmTime,
                    CurrentVideoName = videoName, // 设置录像文件名
                    IsFirstRecordGenerated = true // 标记第一条记录已生成
                };

                // 第四步：添加到活跃报警字典
                _activeAlarms.TryAdd(alarmEvent.ZoneId, newAlarmInfo);

                // 第五步：创建录像信息
                var newVideoInfo = new VideoRecordingInfo
                {
                    ZoneId = alarmEvent.ZoneId,
                    RadarIp = alarmEvent.RadarIp,
                    IsRecording = !string.IsNullOrEmpty(videoName), // 如果成功获取到录像文件名，则标记为正在录像
                    LastVideoStartTime = DateTime.Now
                };
                _videoRecordings.TryAdd(alarmEvent.ZoneId, newVideoInfo);

                _logger.LogInformation($"新报警开始: 防区 {alarmEvent.ZoneId}, 目标 {alarmEvent.TargetId}, AlarmId: {newAlarmRecord.Id}, 录像文件: {videoName}");
            }
            else
            {
                // 只更新最后收到数据的时间
                alarmInfo.LastDataTime = alarmEvent.AlarmTime;
            }
        }

        /// <summary>
        /// 创建报警记录
        /// </summary>
        private async Task<AlarmModel> CreateAlarmRecordAsync(string zoneId, string radarIp,
            DateTime alarmTime, string videoName, bool isEnd = false)
        {
            try
            {
                var now = DateTime.Now;
                WDefenceArea _defenceArea = DefenceAreaManager.GetInstance()[int.Parse(zoneId)];

                var newRecord = new AlarmModel
                {
                    AreaId = int.Parse(zoneId),
                    AreaName = _defenceArea.Name,
                    DateTime = now,
                    StartTime = alarmTime,
                    RecordTime = now,
                    LastRecordTime = now,
                    RadarIp = radarIp,
                    IsActive = isEnd ? "false" : "true",
                    VideoName = videoName, // 保存录像文件名
                    CameraIp = GetCameraIpForZone(zoneId),
                    Latitude = _defenceArea.Latitude,
                    Longitude = _defenceArea.Longitude,
                };

                // 保存到数据库并返回ID
                var result = await _alarmService.AddAlarmAndReturnIdAsync(newRecord);
                if (result > 0)
                {
                    newRecord.Id = result;
                }

                _logger.LogDebug($"创建报警记录: 防区 {zoneId}, AlarmId: {newRecord.Id}, 记录时间: {now}, 录像文件: {videoName}, 结束标记: {isEnd}");

                return newRecord;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建报警记录失败: 防区 {zoneId}");
                return null;
            }
        }

        /// <summary>
        /// 生成报警记录（用于周期性生成）
        /// </summary>
        private async Task GenerateAlarmRecord(string zoneId, ActiveAlarmInfo alarmInfo, bool isEnd = false)
        {
            var newAlarmRecord = await CreateAlarmRecordAsync(zoneId, alarmInfo.RadarIp,
                alarmInfo.StartTime, alarmInfo.CurrentVideoName, isEnd);

            if (newAlarmRecord != null && newAlarmRecord.Id > 0)
            {
                // 更新当前AlarmId
                alarmInfo.CurrentAlarmId = newAlarmRecord.Id;

                // 处理待处理的雷达数据，更新它们的AlarmId
                if (alarmInfo.PendingTrackInfos.Count > 0)
                {
                    foreach (var trackInfo in alarmInfo.PendingTrackInfos)
                    {
                        trackInfo.AlarmId = newAlarmRecord.Id;
                    }
                }
            }
        }

        /// <summary>
        /// 定时生成报警记录和录像（每15秒）
        /// </summary>
        private async Task GenerateAlarmRecordsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;

                    foreach (var alarmInfo in _activeAlarms.Values)
                    {
                        // 检查是否需要生成记录（距离上次生成记录超过15秒）
                        if ((now - alarmInfo.LastGeneratedRecordTime).TotalSeconds >= _recordIntervalSeconds)
                        {
                            // 处理录像逻辑（重启录像）
                            string newVideoName = HandleVideoRecordingForPeriodic(alarmInfo.ZoneId, alarmInfo.RadarIp);

                            // 更新录像文件名
                            if (!string.IsNullOrEmpty(newVideoName))
                            {
                                alarmInfo.CurrentVideoName = newVideoName;
                            }

                            // 生成新的报警记录
                            await GenerateAlarmRecord(alarmInfo.ZoneId, alarmInfo, false);

                            // 更新上次生成记录的时间
                            alarmInfo.LastGeneratedRecordTime = now;
                        }
                    }

                    // 清理长时间不活动的报警（例如：10秒没有收到数据）
                    var expiredTime = now.AddSeconds(-10);
                    var expiredAlarms = _activeAlarms.Where(kv =>
                        kv.Value.LastDataTime < expiredTime).ToList();

                    foreach (var expired in expiredAlarms)
                    {
                        await EndAlarmAsync(expired.Key);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "生成报警记录失败");
                }

                // 等待1秒后再次检查
                await Task.Delay(1000, cancellationToken);
            }
        }

        /// <summary>
        /// 处理周期性的录像逻辑（每15秒重启录像）
        /// </summary>
        private string HandleVideoRecordingForPeriodic(string zoneId, string radarIp)
        {
            // 检查是否有录像信息
            if (_videoRecordings.TryGetValue(zoneId, out var videoInfo))
            {
                var now = DateTime.Now;

                // 检查是否需要开始/重启录像
                // 如果当前没有在录像，或者距离上次开始录像已超过录像周期
                if (!videoInfo.IsRecording ||
                    (now - videoInfo.LastVideoStartTime).TotalSeconds >= _recordIntervalSeconds)
                {
                    // 如果已经在录像，先停止当前的录像
                    if (videoInfo.IsRecording)
                    {
                        StopVideoRecording(zoneId);
                    }

                    // 开始新的录像
                    string newVideoName = StartVideoRecording(int.Parse(zoneId), radarIp);

                    // 更新录像信息
                    videoInfo.IsRecording = !string.IsNullOrEmpty(newVideoName);
                    videoInfo.LastVideoStartTime = now;
                    videoInfo.RadarIp = radarIp;

                    _logger.LogDebug($"重启录像: 防区 {zoneId}, 新录像文件: {newVideoName}, 时间: {now}");

                    return newVideoName;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 开始录像
        /// </summary>
        private string StartVideoRecording(int zoneId, string radarIp)
        {
            try
            {
                if (string.IsNullOrEmpty(GlobalVariable.FilePath))
                {
                    SysConfigService sysConfigService = new SysConfigService();
                    GlobalVariable.FilePath = sysConfigService.GetSysConfigByKey("filepath").ConfigValue;
                }

                WCamera[] cameras = CameraManager.GetInstance().GetBindingCameraOfDefenceArea(zoneId);

                string videoname = string.Empty;
                if (cameras.Length > 0)
                {
                    videoname = VideoAddress.CreateVideoName(cameras[0].Ip, GlobalVariable.FilePath);
                    cameras[0].StartSaveFile(videoname, cameras[0]);
                    _logger.LogInformation($"开始录像成功: 防区 {zoneId}, 录像文件: {videoname}");
                }
                else
                {
                    _logger.LogWarning($"防区 {zoneId} 没有绑定的相机，无法开始录像");
                }

                return videoname;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"开始录像失败: 防区 {zoneId}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 停止录像
        /// </summary>
        private void StopVideoRecording(string zoneId)
        {
            try
            {
                WCamera[] cameras = CameraManager.GetInstance().GetBindingCameraOfDefenceArea(int.Parse(zoneId));

                if (cameras.Length > 0)
                {
                    cameras[0].StopSaveFile(cameras[0]);
                    _logger.LogInformation($"停止录像: 防区 {zoneId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"停止录像失败: 防区 {zoneId}");
            }
        }

        /// <summary>
        /// 根据防区ID获取绑定的相机IP
        /// </summary>
        private string GetCameraIpForZone(string zoneId)
        {
            try
            {
                WCamera[] cameras = CameraManager.GetInstance().GetBindingCameraOfDefenceArea(int.Parse(zoneId));
                if (cameras.Length > 0)
                {
                    return cameras[0].Ip;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取防区 {zoneId} 的相机IP失败");
            }

            return string.Empty;
        }
    }

    // 扩展AlarmService以支持返回ID
    //public partial class AlarmService
    //{
    //    public async Task<int> AddAlarmAndReturnIdAsync(AlarmModel alarm)
    //    {
    //        try
    //        {
    //            // 使用SqlSugar插入并返回ID
    //            var id = await _db.Insertable(alarm).ExecuteReturnIdentityAsync();
    //            return (int)id;
    //        }
    //        catch (Exception ex)
    //        {
    //            // 日志记录
    //            throw;
    //        }
    //    }
    //}
}