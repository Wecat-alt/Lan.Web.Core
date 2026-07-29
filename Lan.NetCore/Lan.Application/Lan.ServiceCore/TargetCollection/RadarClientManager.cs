using System.Collections.Concurrent;
using Lan.RadarSdk.Core;
using Lan.RadarSdk.Core.Models;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.WebScoket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lan.ServiceCore.TargetCollection;

/// <summary>
/// 基于新版 RadarSdk.Core 的雷达连接管理器（BackgroundService）。
///
/// 应用启动时自动从数据库加载所有 Status==1 的雷达，各自建立 TCP 连接并开始接收目标数据。
/// 完全独立于旧版 NsrSdk/NsrRadar，不修改 RadarManager 内部逻辑。
///
/// 数据流：
/// RadarClient.TargetPacketReceived
///   → OnTargetPacketReceived()
///   → 按 IP 查 RadarManager 中的 WRadar
///   → 填充 WRadar.RadarTargets
///   → RadarManager.OnTargetDetect(wRadar) → 复用全部现有下游管线
/// </summary>
public sealed class RadarClientManager : BackgroundService
{
    private readonly ILogger<RadarClientManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<string, RadarClient> _clients = new();
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _receiveCts = new();

    // 雷达状态缓存（在线状态 + 型号）—— static 确保 HostedService 和 Singleton 两个 DI 实例共享同一份数据
    private static readonly ConcurrentDictionary<string, RadarStatusCache> _statusCache = new();

    // 连接/重连参数
    private const int ReconnectDelayMs = 5000;
    private const int DefaultRadarPort = 50000;
    /// <summary>定时状态轮询间隔（秒），用于检测僵死连接</summary>
    private const int StatusPollIntervalSec = 5;

    /// <summary>雷达地址 → 型号名称 映射表</summary>
    private static readonly Dictionary<byte, string> RadarAddressToModel = new()
    {
        [0x60] = "NSR100W",
        [0x02] = "NSR100W",
        [0x08] = "NSR100W",
        [0x07] = "NSR100W_LD",
        [0x90] = "NSR300W",
        [0x04] = "NSR300W",
        [0x10] = "NSR300W",
        [0x70] = "NSR50W",
        [0x03] = "NSR50W",
        [0x17] = "NSR200",
        [0x1D] = "NSR120",
        [0x09] = "NSR150",
        [0x12] = "NSR60W",
        [0x1B] = "WTC261-3000",
        [0x1F] = "WTC261-3000",
        [0x21] = "SUC261",
    };

    /// <summary>雷达状态缓存项</summary>
    private sealed class RadarStatusCache
    {
        public bool Online { get; set; }
        /// <summary>雷达型号名称（如 NSR100W、SUC261），由状态帧中雷达地址映射得出</summary>
        public string? RadarModel { get; set; }
        public DateTime LastStatusTime { get; set; }
    }

    /// <summary>当前已连接的雷达数量</summary>
    public int ConnectedCount => _clients.Count(c => c.Value.IsConnected);

    /// <summary>已管理的雷达 IP 列表</summary>
    public IReadOnlyCollection<string> ManagedIps => _clients.Keys.ToList().AsReadOnly();

    /// <summary>
    /// 查询雷达在线状态。
    /// 在线 = TCP 已连接 且 收到过 0x0A→0xA2 状态应答。
    /// </summary>
    public bool IsOnline(string ip) =>
        _statusCache.TryGetValue(ip, out var cache) && cache.Online;

    /// <summary>
    /// 获取雷达型号名称（如 NSR100W、SUC261）。
    /// 由 0x0A 状态响应中 Raw[0] 雷达地址映射得出。
    /// </summary>
    public string? GetRadarModel(string ip) =>
        _statusCache.TryGetValue(ip, out var cache) ? cache.RadarModel : null;

    public RadarClientManager(ILogger<RadarClientManager> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    // ==================== BackgroundService 生命周期 ====================

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RadarClientManager 正在启动...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var radarService = scope.ServiceProvider.GetRequiredService<IRadarService>();
            var radars = radarService.GetAllList().Where(r => r.Status == 1).ToList();

            _logger.LogInformation("RadarClientManager 发现 {Count} 台状态启用的雷达", radars.Count);

            // 为每台雷达启动独立的后台连接任务
            var tasks = radars.Select(r => Task.Run(() =>
            {
                var ip = r.Ip;
                var port = r.Port > 0 ? r.Port : DefaultRadarPort;
                StartClientInternal(ip, port, stoppingToken);
            }, stoppingToken)).ToList();

            // 等待所有任务完成（即所有雷达都断开或应用关闭）
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RadarClientManager 启动失败");
        }

        _logger.LogInformation("RadarClientManager 已停止");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RadarClientManager 正在停止，断开 {Count} 台雷达...", _clients.Count);

        var stopTasks = _clients.Keys.Select(ip => StopClientAsync(ip));
        await Task.WhenAll(stopTasks);

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        foreach (var cts in _receiveCts.Values)
        {
            try { cts.Dispose(); } catch { }
        }
        _receiveCts.Clear();
        _clients.Clear();
        base.Dispose();
    }

    // ==================== 公开 API（供运行时动态管理雷达） ====================

    /// <summary>手动对单台雷达发起连接（供运行时动态添加雷达使用）</summary>
    public void StartClient(string ip, int port = DefaultRadarPort)
    {
        if (_clients.ContainsKey(ip))
        {
            _logger.LogWarning("雷达 {Ip} 已在管理列表中", ip);
            return;
        }

        // 为动态添加的雷达创建独立的取消令牌
        var cts = new CancellationTokenSource();
        _receiveCts[ip] = cts;
        StartClientInternal(ip, port, cts.Token);
    }

    /// <summary>断开并移除指定雷达</summary>
    public async Task StopClientAsync(string ip)
    {
        if (_receiveCts.TryRemove(ip, out var cts))
        {
            try
            {
                await cts.CancelAsync();
                cts.Dispose();
            }
            catch { }
        }

        if (_clients.TryRemove(ip, out var client))
        {
            try
            {
                await client.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "关闭雷达 {Ip} 客户端时出错", ip);
            }
        }

        _logger.LogInformation("雷达 {Ip} 已断开", ip);
    }

    // ==================== 内部实现 ====================

    private void StartClientInternal(string ip, int port, CancellationToken ct)
    {
        var options = new RadarClientOptions
        {
            IpAddress = ip,
            Port = port,
            PcAddress = 0x10,
            RadarAddress = 0x90
        };

        var client = new RadarClient(options);
        client.TargetPacketReceived += packet => OnTargetPacketReceived(ip, packet);
        client.StatusReceived += status => OnStatusReceived(ip, status);
        client.AckReceived += ack => OnAckReceived(ip, ack);
        client.Log += msg => _logger.LogInformation("[Radar {Ip}] SDK原始日志: {Msg}", ip, msg);

        _clients[ip] = client;

        // 后台启动连接 + 接收循环（RunClientLoopAsync 会阻塞直到取消）
        _ = RunClientLoopAsync(ip, client, ct);
    }

    /// <summary>
    /// 单雷达连接+接收主循环：连接 → 并行[接收循环 + 定时状态轮询] → 异常断开 → 等待 → 重连
    /// - TCP 断线（拔网线/断电）：ReadAsync 立即抛异常，即时检测
    /// - 僵死连接（TCP 通但雷达不应答）：定时 0x0A 轮询，发送失败则主动断开
    /// </summary>
    private async Task RunClientLoopAsync(string ip, RadarClient client, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Step 1: 建立 TCP 连接
                if (!client.IsConnected)
                {
                    _logger.LogInformation("正在连接雷达 {Ip}...", ip);
                    await client.ConnectAsync(ct);
                    _logger.LogInformation("雷达 {Ip} TCP 连接成功 ✓", ip);

                    // 同步系统时间（与老版 C_NsrRadar.SetTime 一致）
                    try
                    {
                        await client.SendSystemTimeAsync(DateTime.Now, ct);
                        _logger.LogDebug("雷达 {Ip} 校时完成", ip);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "雷达 {Ip} 校时失败", ip);
                    }

                    // 发送 0x0A 状态读取命令，验证雷达应用层可达
                    try
                    {
                        await client.SendReadStatusAsync(ct);
                        _logger.LogDebug("雷达 {Ip} 已发送初始状态读取命令 (0x0A)", ip);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "雷达 {Ip} 发送初始状态读取命令失败", ip);
                    }
                }

                // Step 2: 并行运行接收循环 + 定时状态轮询
                using var pollCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                var pollTask = StatusPollLoopAsync(ip, client, pollCts.Token);

                try
                {
                    // 接收循环阻塞直到 TCP 断开或取消
                    await client.ReceiveLoopAsync(ct);
                }
                finally
                {
                    // 接收循环退出，停止轮询
                    pollCts.Cancel();
                    try { await pollTask; } catch (OperationCanceledException) { }
                }
            }
            catch (OperationCanceledException)
            {
                break; // 正常取消
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "雷达 {Ip} 连接异常，{Delay}ms 后自动重连", ip, ReconnectDelayMs);
            }

            // Step 3: 断开清理
            try { await client.DisconnectAsync(); } catch { }

            // 标记离线
            _statusCache.AddOrUpdate(ip,
                _ => new RadarStatusCache { Online = false },
                (_, cache) => { cache.Online = false; return cache; });
            _logger.LogWarning("雷达 {Ip} 已断开，标记为离线", ip);

            if (ct.IsCancellationRequested) break;

            // Step 4: 等待后重连
            try { await Task.Delay(ReconnectDelayMs, ct); }
            catch (OperationCanceledException) { break; }
        }

        _logger.LogInformation("雷达 {Ip} 接收循环退出", ip);
    }

    /// <summary>
    /// 定时向雷达发送 0x0A 状态读取命令。
    /// 发送失败（WriteAsync 抛异常）说明 TCP 连接已断，主动 Disconnect 触发重连。
    /// 这是对 TCP 僵死连接（拔雷达电源但 OS 未感知）的兜底检测。
    /// </summary>
    private async Task StatusPollLoopAsync(string ip, RadarClient client, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(StatusPollIntervalSec));

        // 首次轮询延迟一半间隔，给初始连接的状态读取留出响应时间
        try { await Task.Delay(TimeSpan.FromSeconds(StatusPollIntervalSec / 2), ct); }
        catch (OperationCanceledException) { return; }

        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                await client.SendReadStatusAsync(ct);
                _logger.LogDebug("雷达 {Ip} 定时状态轮询已发送 (0x0A)", ip);
            }
            catch (OperationCanceledException)
            {
                return; // 正常取消
            }
            catch (Exception ex)
            {
                // 发送失败 = TCP 连接已死，主动断开触发外层重连
                _logger.LogWarning(ex, "雷达 {Ip} 定时状态轮询发送失败，主动断开连接触发重连", ip);
                try { await client.DisconnectAsync(); } catch { }
                return; // 退出轮询 → 外层 catch 捕获 ReceiveLoopAsync 异常 → 重连
            }
        }
    }

    /// <summary>
    /// 新版 SDK 目标数据回调 → 适配 → 喂入旧版事件链。
    /// 此方法在 RadarClient 的消费线程上同步执行，确保数据顺序处理。
    /// </summary>
    private void OnTargetPacketReceived(string ip, RadarTargetPacket packet)
    {
        try
        {
            _logger.LogInformation("━━━━ [雷达 {Ip}] 收到 {Count} 个目标 命令字=0x{Cmd:X2} 时间={Time} ━━━━",
                ip, packet.Targets.Count, packet.Command, packet.CaptureTime.ToString("yyyy-MM-dd HH:mm:ss:ffffff"));

            for (int i = 0; i < packet.Targets.Count; i++)
            {
                var t = packet.Targets[i];
                var dist = MathF.Sqrt(t.XAxis * t.XAxis + t.YAxis * t.YAxis);
                _logger.LogInformation(
                    "  [#{Idx}] Id={Id} Type={Type} X={X:F2} Y={Y:F2} Z={Z:F2} Dist={Dist:F2}m " +
                    "SpeedX={Sx:F2} SpeedY={Sy:F2} Azimuth={Az:F2}° Elevation={Elev:F2}° SNR={Snr:F1} Time={Time}",
                    i + 1, t.Id, t.Type, t.XAxis, t.YAxis, t.ZAxis, dist,
                    t.XSpeed, t.YSpeed, t.AzimuthAngle, t.ElevationAngle, t.Snr,
                    t.CaptureTime.ToString("yyyy-MM-dd HH:mm:ss:ffffff"));
            }

            // 1. 从 RadarManager 获取对应 WRadar
            var wRadar = RadarManager.GetInstance()?[ip];
            if (wRadar == null)
            {
                _logger.LogWarning("收到目标数据但未找到雷达 {Ip} 的 WRadar 注册", ip);
                return;
            }

            // 2. 将新版 RadarTarget 适配为旧版 IRvs_Target[]
            var adaptedTargets = new IRvs_Target[packet.Targets.Count];
            for (int i = 0; i < adaptedTargets.Length; i++)
            {
                adaptedTargets[i] = new RadarTargetAdapter(packet.Targets[i]);
            }

            // 3. 构造旧版 RVS_Target_List 并填入 WRadar
            var targetList = new RVS_Target_List(adaptedTargets, packet.CaptureTime);
            wRadar.RadarTargets = targetList;

            // 4. 触发 RadarManager 事件 → DefenceAreaManager.TargetDetectCallback
            //    → WDefenceArea.AddAlarmTarget → TargetCollection.AddTarget
            //    → 报警队列 + SignalR 推送 + 数据库写入（全部复用现有管线）
            RadarManager.GetInstance()?.OnTargetDetect(wRadar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理雷达 {Ip} 目标数据失败", ip);
        }
    }

    /// <summary>
    /// 收到雷达状态响应（0x0A 命令的 0xA2 ACK 或 0xAA 主动上报）。
    /// 收到即表示雷达在线，同时从雷达地址解析型号名称。
    ///
    /// Raw[0] = 雷达地址，映射到型号名称（如 0x60→NSR100W, 0x21→SUC261）
    /// </summary>
    /// <summary>
    /// ACK 帧日志：打印完整帧头信息。
    /// </summary>
    private void OnAckReceived(string ip, RadarAck ack)
    {
        _logger.LogInformation(
            "[雷达 {Ip}] ACK帧 → Src=0x{Src:X2} Dst=0x{Dst:X2} AckCmd=0x{AckCmd:X2} Result=0x{Result:X2}",
            ip, ack.SourceAddress, ack.DestinationAddress, ack.AcknowledgedCommand, ack.ResultCode);
    }

    private void OnStatusReceived(string ip, RadarStatus status)
    {
        // 打印完整 Raw 数据（十六进制），方便验证字节布局
        var hexDump = BitConverter.ToString(status.Raw);
        _logger.LogInformation("[雷达 {Ip}] 状态帧 Raw({Len}字节): {Hex}", ip, status.Raw.Length, hexDump);

        // 从 Raw[8-9] 解析雷达型号代码（完整帧 byte 17-18，不含帧头 byte 15-16，little-endian short）
        short? modelCode = null;
        string? modelName = null;
        if (status.Raw.Length > 9)
        {
            modelCode = (short)(status.Raw[8] | (status.Raw[9] << 8));
            RadarAddressToModel.TryGetValue((byte)modelCode.Value, out modelName);
        }

        _statusCache.AddOrUpdate(ip,
            _ => new RadarStatusCache { Online = true, RadarModel = modelName, LastStatusTime = DateTime.UtcNow },
            (_, cache) =>
            {
                cache.Online = true;
                if (!string.IsNullOrEmpty(modelName)) cache.RadarModel = modelName;
                cache.LastStatusTime = DateTime.UtcNow;
                return cache;
            });

        _logger.LogInformation("雷达 {Ip} Raw[8-9]=0x{Code:X4} → {Model}",
            ip, modelCode ?? 0, modelName ?? "未匹配");
    }
}
