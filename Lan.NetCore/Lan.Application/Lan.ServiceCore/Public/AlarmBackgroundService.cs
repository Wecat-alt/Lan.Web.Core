using Dm;
using Lan.Infrastructure;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.Services;
using Lan.ServiceCore.Signalr;
using Lan.ServiceCore.TargetCollection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Model;
using Newtonsoft.Json;
using SqlSugar;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Lan.ServiceCore.Public
{
    public class AlarmBackgroundService : BackgroundService
    {
        AlarmService alarmService = new AlarmService();
        CameraService cameraService = new CameraService();

        private readonly ILogger<AlarmBackgroundService> _logger;
        private readonly IHubContext<MessageHub> _messageHub;
        private readonly BlockingCollection<AlarmEvent> _alarmQueue = new();
        private readonly ConcurrentDictionary<string, ActiveAlarmInfo> _activeAlarms = new();
        private readonly ConcurrentDictionary<string, VideoRecordingInfo> _videoRecordings = new();
        private readonly int _recordIntervalSeconds = 15; // 默认15秒，也是录像间隔

        private static AlarmBackgroundService _instance;
        public static AlarmBackgroundService Instance => _instance;

        // 内部类，用于跟踪活跃报警的信息
        private class ActiveAlarmInfo
        {
            public string ZoneId { get; set; }
            public string RadarIp { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime LastGeneratedRecordTime { get; set; } // 上次生成记录的时间
            public DateTime LastDataTime { get; set; } // 最后收到数据的时间
            public string CurrentVideoName { get; set; } // 当前录像文件名
            public bool IsFirstRecordGenerated { get; set; } = false; // 标记第一条记录是否已生成
        }

        // 内部类，用于跟踪录像信息
        private class VideoRecordingInfo
        {
            public string ZoneId { get; set; }
            public string RadarIp { get; set; }
            public DateTime LastVideoStartTime { get; set; } // 上次开始录像的时间
            public bool IsRecording { get; set; } // 是否正在录像
        }

        public AlarmBackgroundService(ILogger<AlarmBackgroundService> logger, IHubContext<MessageHub> messageHub)
        {
            _instance = this; // 设置静态实例
            _logger = logger;
            _messageHub = messageHub;
        }

        public void Write(AlarmEvent alarmEvent)
        {
            _alarmQueue.Add(alarmEvent);
        }

        /// <summary>
        /// 结束指定防区的报警
        /// </summary>
        public async Task EndAlarmAsync(string zoneId)
        {
            if (_activeAlarms.TryRemove(zoneId, out var alarmInfo))
            {
                // 如果有录像，在生成最后一条记录前确保录像文件名已更新
                if (_videoRecordings.TryGetValue(zoneId, out var videoInfo) && videoInfo.IsRecording)
                {
                    // 停止录像并获取最终文件名
                    StopVideoRecording(zoneId);

                    // 如果有录像，确保录像文件名已更新到alarmInfo中
                    if (!string.IsNullOrEmpty(alarmInfo.CurrentVideoName))
                    {
                        // 生成一条结束记录
                        GenerateAlarmRecord(zoneId, alarmInfo, true);
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
            // 启动两个并行任务
            var processTask = Task.Run(() => ProcessAlarmQueueAsync(stoppingToken));
            var recordTask = Task.Run(() => GenerateAlarmRecordsAsync(stoppingToken));

            await Task.WhenAll(processTask, recordTask);
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
                    ProcessSingleAlarm(alarmEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"处理报警事件失败: {alarmEvent.ZoneId}");
                }
            }
        }

        /// <summary>
        /// 处理单个报警事件（只更新状态，不生成记录）
        /// </summary>
        private void ProcessSingleAlarm(AlarmEvent alarmEvent)
        {
            // 检查是否已有活跃报警
            if (!_activeAlarms.TryGetValue(alarmEvent.ZoneId, out var alarmInfo))
            {
                // 第一步：立即开始录像并获取录像文件名
                string videoName = StartVideoRecording(int.Parse(alarmEvent.ZoneId), alarmEvent.RadarIp);

                // 第二步：创建新的活跃报警信息
                var newAlarmInfo = new ActiveAlarmInfo
                {
                    ZoneId = alarmEvent.ZoneId,
                    RadarIp = alarmEvent.RadarIp,
                    StartTime = alarmEvent.AlarmTime,
                    LastGeneratedRecordTime = alarmEvent.AlarmTime,
                    LastDataTime = alarmEvent.AlarmTime,
                    CurrentVideoName = videoName, // 设置录像文件名
                    IsFirstRecordGenerated = false // 标记第一条记录未生成
                };

                // 第三步：立即生成第一条报警记录（此时已有录像文件名）
                GenerateAlarmRecord(alarmEvent.ZoneId, newAlarmInfo, false);
                newAlarmInfo.IsFirstRecordGenerated = true; // 标记第一条记录已生成
                newAlarmInfo.LastGeneratedRecordTime = DateTime.Now; // 更新最后生成记录时间

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

                _logger.LogInformation($"新报警开始: 防区 {alarmEvent.ZoneId}, 目标 {alarmEvent.TargetId}, 录像文件: {videoName}");
            }
            else
            {
                // 只更新最后收到数据的时间
                alarmInfo.LastDataTime = alarmEvent.AlarmTime;
            }
        }

        /// <summary>
        /// 生成报警记录
        /// </summary>
        private void GenerateAlarmRecord(string zoneId, ActiveAlarmInfo alarmInfo, bool isEnd = false)
        {
            var now = DateTime.Now;

            WDefenceArea _defenceArea = DefenceAreaManager.GetInstance()[int.Parse(zoneId)];
            if (_defenceArea is not null)
            {
                var newRecord = new AlarmModel
                {
                    AreaId = int.Parse(zoneId),
                    AreaName = _defenceArea.Name,
                    DateTime = now,
                    StartTime = alarmInfo.StartTime,
                    RecordTime = now,
                    LastRecordTime = alarmInfo.LastDataTime,
                    RadarIp = alarmInfo.RadarIp,
                    IsActive = isEnd ? "false" : "true",
                    VideoName = alarmInfo.CurrentVideoName,
                    CameraIp = GetCameraIpForZone(zoneId),
                    Latitude = _defenceArea.Latitude,
                    Longitude = _defenceArea.Longitude,
                };

                alarmService.AddAlarm(newRecord);
                SendAlarmPopupIfValid(newRecord);

                _logger.LogDebug($"生成报警记录: 防区 {zoneId}, 记录时间: {now}, 录像文件: {alarmInfo.CurrentVideoName}, 结束标记: {isEnd}");
            }
        }

        private void SendAlarmPopupIfValid(AlarmModel newRecord)
        {
            var cameraInfo = string.IsNullOrWhiteSpace(newRecord.CameraIp)
                ? null
                : cameraService.GetInfo(newRecord.CameraIp);

            var canSendPopup = cameraInfo != null
                && !string.IsNullOrWhiteSpace(cameraInfo.Ip)
                && !string.IsNullOrWhiteSpace(cameraInfo.Username)
                && !string.IsNullOrWhiteSpace(cameraInfo.Password)
                && !string.IsNullOrWhiteSpace(cameraInfo.CameraURL);

            if (!canSendPopup)
            {
                _logger.LogWarning("报警弹窗未发送：相机信息不存在或字段为空，AreaId: {AreaId}, CameraIp: {CameraIp}", newRecord.AreaId, newRecord.CameraIp);
                return;
            }

            var popupJson = JsonConvert.SerializeObject(new
            {
                CameraIp = cameraInfo.Ip,
                Username = cameraInfo.Username,
                Password = cameraInfo.Password,
                CameraURL = cameraInfo.CameraURL
            });

            _ = _messageHub.Clients.All.SendAsync("AlarmPopup", popupJson)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                    {
                        _logger.LogError(t.Exception, "发送报警弹窗消息失败");
                    }
                });
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
                            // 对于第一条记录，由于在ProcessSingleAlarm中已经处理，这里跳过
                            if (!alarmInfo.IsFirstRecordGenerated)
                            {
                                // 理论上不应该走到这里，但为了安全起见
                                continue;
                            }

                            // 处理录像逻辑（重启录像）
                            string newVideoName = HandleVideoRecordingForPeriodic(alarmInfo.ZoneId, alarmInfo.RadarIp);

                            // 更新录像文件名
                            if (!string.IsNullOrEmpty(newVideoName))
                            {
                                alarmInfo.CurrentVideoName = newVideoName;
                            }

                            // 生成报警记录
                            GenerateAlarmRecord(alarmInfo.ZoneId, alarmInfo, false);

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
        /// 开始录像（具体实现由你完成）
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
        /// 停止录像（具体实现由你完成）
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
}