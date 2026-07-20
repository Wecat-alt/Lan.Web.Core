namespace Lan.RadarSdk.Core.Models;

public readonly record struct RadarAck(byte SourceAddress, byte DestinationAddress, byte AcknowledgedCommand, byte ResultCode)
{
    public bool IsSuccess => ResultCode == 0x0F;
}