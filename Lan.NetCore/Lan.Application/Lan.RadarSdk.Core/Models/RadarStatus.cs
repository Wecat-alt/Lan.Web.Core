namespace Lan.RadarSdk.Core.Models;

public sealed class RadarStatus
{
    public required byte[] Raw { get; init; }
    public byte? LocalAddress => Raw.Length >= 1 ? Raw[0] : null;
    public byte? HeartbeatIntervalSeconds => Raw.Length >= 2 ? Raw[1] : null;
    public byte? RelayStatus => Raw.Length >= 3 ? Raw[2] : null;
    public byte? AppVersionRaw => Raw.Length >= 4 ? Raw[3] : null;
}