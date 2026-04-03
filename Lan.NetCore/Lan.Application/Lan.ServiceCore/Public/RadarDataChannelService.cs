using Dm.util;
using Lan.ServiceCore.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Model;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Lan.ServiceCore.Public
{
    public class RadarDataChannelService : BackgroundService
    {
        TrackInfoService trackInfoService = new TrackInfoService();
        private static RadarDataChannelService _instance;
        public static RadarDataChannelService Instance => _instance;

        private readonly Channel<TrackInfo> _channel;
        private readonly ILogger<RadarDataChannelService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private const int CHANNEL_CAPACITY = 10000;
        private const int BATCH_SIZE = 200;

        public RadarDataChannelService(ILogger<RadarDataChannelService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _instance = this; 
            var options = new BoundedChannelOptions(CHANNEL_CAPACITY)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };
            _channel = Channel.CreateBounded<TrackInfo>(options);
        }

        public async ValueTask WriteAsync(TrackInfo smsData, CancellationToken cancellationToken = default)
        {
            await _channel.Writer.WriteAsync(smsData, cancellationToken);
        }

        public void Write(TrackInfo smsData)
        {
            if (!_channel.Writer.TryWrite(smsData))
            {
                _logger.LogWarning("通道已满，无法写入数据，目标ID: {TargetId}", smsData.TargetId);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("雷达数据Channel服务启动");

            var batch = new List<TrackInfo>(BATCH_SIZE);
            var timeout = TimeSpan.FromSeconds(10); // 超时时间1秒

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 等待数据，带超时
                    var hasData = await _channel.Reader.WaitToReadAsync(stoppingToken).AsTask().WaitAsync(timeout, stoppingToken);

                    if (hasData)
                    {
                        // 有数据，读取所有可读的数据
                        while (_channel.Reader.TryRead(out var data))
                        {
                            batch.Add(data);
                            if (batch.Count >= BATCH_SIZE)
                            {
                                await ProcessBatchAsync(batch);
                                batch.Clear();
                            }
                        }
                    }
                    else
                    {
                        // 超时，处理剩余数据
                        if (batch.Count > 0)
                        {
                            await ProcessBatchAsync(batch);
                            batch.Clear();
                        }
                    }
                }
                catch (TimeoutException)
                {
                    // 超时，处理剩余数据
                    if (batch.Count > 0)
                    {
                        await ProcessBatchAsync(batch);
                        batch.Clear();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理雷达数据时发生异常");
                }
            }

            // 服务停止时处理剩余数据
            if (batch.Count > 0) await ProcessBatchAsync(batch);
        }

        private async Task ProcessBatchAsync(List<TrackInfo> batch)
        {
            if (batch.Count == 0) return;

            try
            {
                var result = await trackInfoService.BatchInsertAsync(batch);

                _logger.LogInformation("成功批量插入 {Count} 条雷达数据，影响行数: {Rows}", batch.Count, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量插入雷达数据失败，数据量: {Count}", batch.Count);
            }
        }


        //private void SaveToBackupFileSync(List<TrackInfo> batch)
        //{
        //    try
        //    {
        //        var backupDir = "DataBackup";
        //        if (!Directory.Exists(backupDir))
        //            Directory.CreateDirectory(backupDir);

        //        var fileName = $"radar_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        //        var filePath = Path.Combine(backupDir, fileName);

        //        var json = JsonSerializer.Serialize(batch, new JsonSerializerOptions
        //        {
        //            WriteIndented = false // 不格式化以减少文件大小
        //        });

        //        File.WriteAllText(filePath, json);
        //        _logger.LogWarning("雷达数据已备份到文件: {FilePath}", filePath);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "备份雷达数据到文件失败");
        //    }
        //}

    }
}
