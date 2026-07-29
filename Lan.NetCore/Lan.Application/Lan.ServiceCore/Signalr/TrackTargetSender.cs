using Microsoft.AspNetCore.SignalR;

namespace Lan.ServiceCore.Signalr;

/// <summary>
/// 跟踪目标 ID 推送器。由 RBTrack SDK 回调直接 fire-and-forget 发送，不经过队列。
/// </summary>
public static class TrackTargetSender
{
    private static IHubContext<MessageHub>? _hubContext;

    public static void Initialize(IHubContext<MessageHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public static void Send(int targetId)
    {
        _ = _hubContext?.Clients.All.SendAsync("TrackTargetData", targetId);
    }
}
