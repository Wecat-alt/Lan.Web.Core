namespace Lan.RadarSdk.Core.Models;

/// <summary>
/// 单个目标的数据模型。
/// A8 / A9 两种目标上传协议的单目标结构一致，均使用该模型承载。
/// </summary>
public sealed class RadarTarget
{
    public uint Id { get; init; }
    public uint Type { get; init; }
    public float XSpeed { get; init; }
    public float YSpeed { get; init; }
    public float ZSpeed { get; init; }
    public float XAxis { get; init; }
    public float YAxis { get; init; }
    public float ZAxis { get; init; }
    public float Length { get; init; }
    public float AzimuthAngle { get; init; }
    public float ElevationAngle { get; init; }
    public float Snr { get; init; }
    public float PeakEnergy { get; init; }
    public ushort Area { get; init; }
    public required byte[] Reserved { get; init; }

    /// <summary>目标捕获时间（帧解析完成时刻），精确到微秒。</summary>
    public DateTime CaptureTime { get; init; }
}