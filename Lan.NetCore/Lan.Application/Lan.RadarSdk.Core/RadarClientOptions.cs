namespace Lan.RadarSdk.Core;

public sealed class RadarClientOptions
{
    public string IpAddress { get; init; } = "192.168.11.183";
    public int Port { get; init; } = 50000;
    public byte PcAddress { get; init; } = 0x10;
    public byte RadarAddress { get; init; } = 0x90;
}