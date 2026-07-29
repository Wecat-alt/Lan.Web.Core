// 从旧版 Lan.NsrRadarSdk 迁移过来的类型定义。
// 移除旧 SDK 后，这些类型保留在 ServiceCore 内部继续使用。
namespace Lan.ServiceCore.TargetCollection;

/// <summary>雷达目标接口（从 NsrRadarSdk 迁移）</summary>
public interface IRvs_Target
{
    uint Id { get; }
    uint Type { get; }
    float X { get; }
    float Y { get; }
    float SpeedX { get; }
    float SpeedY { get; }
    float SpeedZ { get; }
    float AxesX { get; }
    float AxesY { get; }
    float AxesZ { get; }
    float Distance { get; }
    float AzimuthAngle { get; }
    float ElevationAngle { get; }
    float Snr { get; }
    float PeakEnergy { get; }

    /// <summary>目标捕获时间（帧解析完成时刻），精确到微秒。</summary>
    DateTime CaptureTime { get; }
}

/// <summary>雷达目标列表（从 NsrRadarSdk 迁移，简化版）</summary>
public class RVS_Target_List
{
    public int TargetNum { get; set; }

    public IRvs_Target[] Targets { get; private set; }

    /// <summary>帧捕获时间（解析完成时刻），精确到微秒。</summary>
    public DateTime CaptureTime { get; set; }

    public RVS_Target_List(IRvs_Target[] targets)
    {
        TargetNum = targets.Length;
        Targets = targets;
    }

    public RVS_Target_List(IRvs_Target[] targets, DateTime captureTime) : this(targets)
    {
        CaptureTime = captureTime;
    }
}

/// <summary>雷达设备地址（旧版，从 NsrRadarSdk 迁移）</summary>
public enum RVS_DeviceAddress : byte
{
    Unknown = 0,
    PC_ADDR = 0x10,
    RADAR_ADDR = 0x40,
    WVF_RADAR_100_ADDR = 0x60,
    WVF_RADAR_50_ADDR = 0x70,
    LASER_ADDR = 0x80,
    WVF_RADAR_300_ADDR = 0x90,
    BroadCast = 0xFF,
}

/// <summary>雷达设备地址（新版，从 NsrRadarSdk 迁移）</summary>
public enum RVS_DeviceAddressNEW : byte
{
    Unknown = 0,
    PC_ADDR = 0x10,
    RADAR_ADDR = 0x40,
    RADAR_ADDR_new = 0x01,

    WVF_RADAR_100W_ADDR = 0x60,
    WVF_RADAR_100W_new_ADDR = 0x02,
    WVF_RADAR_300W_ADDR = 0x90,
    WVF_RADAR_300W_new_ADDR = 0x04,
    WVF_RADAR_300_M_ADDR = 0x10,
    WVF_RADAR_100W_LD_ADDR = 0x07,
    WVF_RADAR_100W_M_ADDR = 0x08,
    WVF_RADAR_NSR50W_ADDR = 0x70,
    WVF_RADAR_50W_new_ADDR = 0x03,
    WVF_RADAR_NSR200_ADDR = 0x17,
    WVF_RADAR_NSR120_ADDR = 0x1D,
    WVF_RADAR_NSR150_ADDR = 0x09,
    WVF_RADAR_NSR60W_ADDR = 0x12,
    WVF_RADAR_WTC261_3000_800_ADDR = 0x18,
    LASER_ADDR = 0x80,
    BroadCast = 0xFF,
}
