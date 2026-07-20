namespace Lan.RadarSdk.Core.Models;

/// <summary>
/// 目标上传包的数据模型。
/// 用于统一承载 A8 / A9 两类目标上传帧。
/// </summary>
public sealed class RadarTargetPacket
{
    public required byte Command { get; init; }
    public required byte SourceAddress { get; init; }
    public required byte DestinationAddress { get; init; }
    public required ushort ParamLength { get; init; }
    public required uint TargetCount { get; init; }
    public required IReadOnlyList<RadarTarget> Targets { get; init; }
}
