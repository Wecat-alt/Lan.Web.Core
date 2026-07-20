using Lan.RadarSdk.Core.Models;

namespace Lan.ServiceCore.TargetCollection;

/// <summary>
/// 将新版 RadarSdk.Core 的 RadarTarget 适配为旧版 IRvs_Target 接口，
/// 使 TargetCollection.AddTarget() 无需任何修改即可消费新版 SDK 数据。
/// </summary>
internal sealed class RadarTargetAdapter : IRvs_Target
{
    private readonly RadarTarget _t;

    public RadarTargetAdapter(RadarTarget t) => _t = t;

    public uint Id => _t.Id;
    public uint Type => _t.Type;
    /// <summary>X 坐标映射为 XAxis</summary>
    public float X => _t.XAxis;
    /// <summary>Y 坐标映射为 YAxis</summary>
    public float Y => _t.YAxis;
    public float SpeedX => _t.XSpeed;
    public float SpeedY => _t.YSpeed;
    public float SpeedZ => _t.ZSpeed;
    public float AxesX => _t.XAxis;
    public float AxesY => _t.YAxis;
    public float AxesZ => _t.ZAxis;
    public float Distance => MathF.Sqrt(_t.XAxis * _t.XAxis + _t.YAxis * _t.YAxis);
    public float AzimuthAngle => _t.AzimuthAngle;
    public float ElevationAngle => _t.ElevationAngle;
    public float Snr => _t.Snr;
    public float PeakEnergy => _t.PeakEnergy;
}
