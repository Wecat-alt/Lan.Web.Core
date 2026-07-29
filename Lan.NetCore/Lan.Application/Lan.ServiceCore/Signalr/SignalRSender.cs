using Lan.ServiceCore.TargetCollection;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Concurrent;

namespace Lan.ServiceCore.Signalr;

/// <summary>
/// 帧级批量 SignalR 发送器。按雷达 IP 独立计时，每隔1秒最多发送一帧。
/// </summary>
public static class SignalRSender
{
    private static IHubContext<MessageHub>? _hubContext;
    private static readonly ConcurrentDictionary<string, DateTime> _lastSendTime = new();
    private static readonly JsonSerializerSettings _settings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public static void Initialize(IHubContext<MessageHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <summary>
    /// 按雷达 IP 独立计时，距上次发送 ≥ 1 秒才发送。帧数据为空则跳过。
    /// </summary>
    public static void SendFrameIfNeeded(string radarIp, List<SendMS> frame)
    {
        if (_hubContext == null || frame == null || frame.Count == 0)
            return;

        var now = DateTime.Now;
        var last = _lastSendTime.GetOrAdd(radarIp, DateTime.MinValue);

        if ((now - last).TotalSeconds < 1)
            return;

        _lastSendTime[radarIp] = now;

        string json = JsonConvert.SerializeObject(frame, _settings);
        _ = _hubContext.Clients.All.SendAsync("ReceiveTargetData", json);
    }
}
