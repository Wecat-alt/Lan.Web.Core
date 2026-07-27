using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Threading.Channels;
using Lan.RadarSdk.Core.Models;

namespace Lan.RadarSdk.Core;

/// <summary>
/// 与雷达设备通信的轻量级 TCP 客户端。
///
/// 职责：
/// - 建立与雷达的 TCP 连接
/// - 发送命令帧（系统时间设置、读取状态或任意原始帧）
/// - 接收原始字节并完成帧边界处理（处理粘包/半包）
/// - 校验校验和（兼容两种常见实现）
/// - 通过事件分发已解析的帧
///
/// 协议说明：
/// - 帧头：0xA5 0x5A
/// - 字节2：源地址（PC）
/// - 字节3：目的地址（雷达）
/// - 字节4：命令字
/// - 字节5-6：参数长度（小端）
/// - 参数区从字节7开始
/// - 最后一字节：校验和（累加）。部分固件将起始码 A5 5A 纳入累加，部分固件不纳入。
/// - 总长度 = 8 + paramLength（8 = 固定头 0..6 + 校验和）
///
/// 该类设计为跨项目复用。公开事件供调用方订阅 ACK、状态和目标上传通知。
///
/// 性能设计：
/// - 接收线程只负责收包、切包并写入通道
/// - 消费线程负责解析帧类型并触发上层事件
/// - 这样可以避免上层事件处理过慢时直接阻塞网络接收线程
/// </summary>
public sealed class RadarClient(RadarClientOptions options) : IAsyncDisposable
{
    // 协议常量
    private const byte StartCode0 = 0xA5;
    private const byte StartCode1 = 0x5A;
    private const int TargetBlockSize = 68;

    // 命令字定义
    private const byte StatusReadCommand = 0x0A;        // PC -> 雷达：读取状态
    private const byte SystemTimeSetCommand = 0x24;     // PC -> 雷达：设置系统时间
    private const byte AckCommand = 0xA2;               // 雷达 -> PC：应答
    private const byte StatusReportCommand = 0xAA;      // 雷达 -> PC：状态上报（部分固件）
    private const byte TargetUploadCommand = 0xA8;      // 雷达 -> PC：A8 目标上传
    private const byte TargetUploadCommandA9 = 0xA9;    // 雷达 -> PC：A9 目标上传（另一种雷达，帧格式一致）

    // 调用方提供的配置
    private readonly RadarClientOptions _options = options;

    // 发送锁：保证同一时刻只发送一个完整帧，避免多线程并发写入导致数据交错
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    // 底层网络对象
    private TcpClient? _client;
    private NetworkStream? _stream;
    private bool _disposed;

    private readonly record struct RadarFrame(byte[] Buffer, int Length)
    {
        public ReadOnlySpan<byte> Span => Buffer.AsSpan(0, Length);
    }

    /// <summary>当接收到雷达的 ACK (0xA2) 时触发。</summary>
    public event Action<RadarAck>? AckReceived;

    /// <summary>当接收到状态负载（来自 0xAA 或 0x0A 的响应）时触发。</summary>
    public event Action<RadarStatus>? StatusReceived;

    /// <summary>当接收到已解析的目标上传包（A8 / A9）时触发。</summary>
    public event Action<RadarTargetPacket>? TargetPacketReceived;

    /// <summary>当观察到目标上传包（0xA8 或 0xA9）时触发。参数为包长度。</summary>
    public event Action<int>? TargetUploadPacketReceived;

    /// <summary>当观察到目标上传包（0xA8 或 0xA9）时触发。参数分别为命令字和包长度。</summary>
    public event Action<byte, int>? TargetUploadPacketDetected;

    /// <summary>通用日志回调（文本），用于诊断。</summary>
    public event Action<string>? Log;

    /// <summary>当前是否已连接到雷达。</summary>
    public bool IsConnected => _client?.Connected == true;

    /// <summary>
    /// 使用配置建立到雷达的 TCP 连接。
    /// 连接成功后即可通过 NetworkStream 进行收发。
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            return;
        }

        var client = new TcpClient();
        await client.ConnectAsync(_options.IpAddress, _options.Port, cancellationToken);

        // 开启 TCP KeepAlive，降低长连接空闲时被中间设备回收连接的概率
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        _client = client;
        _stream = client.GetStream();
    }

    /// <summary>发送系统时间设置命令 (0x24) 到雷达，使用指定的 DateTime。</summary>
    public Task SendSystemTimeAsync(DateTime dt, CancellationToken cancellationToken = default)
    {
        return SendRawAsync(BuildSystemTimeSetPacket(dt), cancellationToken);
    }

    /// <summary>发送状态读取命令 (0x0A) 到雷达。</summary>
    public Task SendReadStatusAsync(CancellationToken cancellationToken = default)
    {
        return SendRawAsync(BuildStatusReadPacket(), cancellationToken);
    }

    /// <summary>低级 API：发送已构建好的帧（字节数组）到雷达。</summary>
    /// <remarks>使用内部锁保证发送的原子性，避免帧被交错。</remarks>
    public async Task SendRawAsync(byte[] packet, CancellationToken cancellationToken = default)
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("Radar client is not connected.");
        }

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await _stream.WriteAsync(packet, cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// 接收循环：持续从网络流读取并解析帧。
    /// 内部使用生产者/消费者模式：
    /// - 生产者负责接收并切出完整帧
    /// - 消费者负责解析命令并触发上层事件
    /// 当流关闭或传入的取消令牌触发时，该方法返回。
    /// </summary>
    public async Task ReceiveLoopAsync(CancellationToken cancellationToken = default)
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("Radar client is not connected.");
        }

        var channel = Channel.CreateUnbounded<RadarFrame>(new UnboundedChannelOptions
        {
            SingleWriter = true,
            SingleReader = true,
            AllowSynchronousContinuations = false
        });

        Task consumerTask = ConsumeFramesAsync(channel.Reader, cancellationToken);

        try
        {
            await ProduceFramesAsync(channel.Writer, cancellationToken);
            channel.Writer.TryComplete();
            await consumerTask;
        }
        catch (Exception ex)
        {
            channel.Writer.TryComplete(ex);
            await consumerTask;
            throw;
        }
    }

    /// <summary>关闭流和 TCP 客户端。可多次安全调用。</summary>
    public Task DisconnectAsync()
    {
        try
        {
            _stream?.Close();
            _client?.Close();
        }
        finally
        {
            _stream = null;
            _client = null;
        }

        return Task.CompletedTask;
    }

    /// <summary>异步释放资源。</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await DisconnectAsync();
        _sendLock.Dispose();
    }

    // -------------------- 帧构建器 --------------------

    /// <summary>构建一个 0x0A 状态读取帧。</summary>
    private byte[] BuildStatusReadPacket()
    {
        // 帧格式: [A5][5A][src][dst][cmd][lenLo][lenHi][checksum]
        byte[] packet = new byte[8];
        packet[0] = StartCode0;
        packet[1] = StartCode1;
        packet[2] = _options.PcAddress;
        packet[3] = _options.RadarAddress;
        packet[4] = StatusReadCommand;
        packet[5] = 0x00;
        packet[6] = 0x00;
        packet[7] = CalcChecksum(packet, 2);
        return packet;
    }

    /// <summary>根据给定 DateTime 构建 0x24 系统时间设置帧。</summary>
    private byte[] BuildSystemTimeSetPacket(DateTime dt)
    {
        byte[] packet = new byte[16];
        packet[0] = StartCode0;
        packet[1] = StartCode1;
        packet[2] = _options.PcAddress;
        packet[3] = _options.RadarAddress;
        packet[4] = SystemTimeSetCommand;
        packet[5] = 0x08;
        packet[6] = 0x00;

        int yearOffset = Math.Clamp(dt.Year - 1970, 0, 0x63);
        packet[7] = (byte)yearOffset;
        packet[8] = (byte)dt.Month;
        packet[9] = (byte)dt.Day;
        packet[10] = (byte)dt.Hour;
        packet[11] = (byte)dt.Minute;
        packet[12] = (byte)dt.Second;
        packet[13] = 0x00;
        packet[14] = 0x00;
        packet[15] = CalcChecksum(packet, 2);

        return packet;
    }

    // -------------------- 生产者 / 消费者 --------------------

    /// <summary>
    /// 生产者：持续从网络流读取数据，并将完整帧写入通道。
    /// 该阶段只做切包和校验，不做业务分发，尽量缩短占用接收线程的时间。
    /// </summary>
    private async Task ProduceFramesAsync(ChannelWriter<RadarFrame> writer, CancellationToken cancellationToken)
    {
        byte[] receiveBuffer = new byte[64 * 1024];
        int bufferedCount = 0;

        while (true)
        {
            if (bufferedCount == receiveBuffer.Length)
            {
                Array.Resize(ref receiveBuffer, receiveBuffer.Length * 2);
            }

            int bytesRead = await _stream!.ReadAsync(receiveBuffer.AsMemory(bufferedCount), cancellationToken);
            if (bytesRead == 0)
            {
                Log?.Invoke("[INFO] Connection closed by radar.");
                return;
            }

            bufferedCount += bytesRead;
            ParsePackets(receiveBuffer, ref bufferedCount, writer);
        }
    }

    /// <summary>
    /// 消费者：从通道中顺序读取完整帧，并执行协议解析与事件分发。
    /// 这样即使上层处理稍慢，也不会直接阻塞网络接收线程。
    /// </summary>
    private async Task ConsumeFramesAsync(ChannelReader<RadarFrame> reader, CancellationToken cancellationToken)
    {
        await foreach (RadarFrame frame in reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                DispatchPacket(frame.Span);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(frame.Buffer);
            }
        }
    }

    // -------------------- 解析 / 帧处理 --------------------

    /// <summary>
    /// 从缓冲区解析帧，处理粘包/半包场景，并将完整帧写入通道。
    /// </summary>
    private void ParsePackets(byte[] buffer, ref int bufferedCount, ChannelWriter<RadarFrame> writer)
    {
        int index = 0;

        while (true)
        {
            int available = bufferedCount - index;
            if (available < 8)
            {
                break;
            }

            int headerOffset = FindHeader(buffer.AsSpan(index, available));
            if (headerOffset < 0)
            {
                if (bufferedCount > 0)
                {
                    buffer[0] = buffer[bufferedCount - 1];
                    bufferedCount = 1;
                }
                else
                {
                    bufferedCount = 0;
                }

                return;
            }

            index += headerOffset;
            available = bufferedCount - index;
            if (available < 8)
            {
                break;
            }

            ushort paramLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(index + 5, 2));
            int packetLength = 8 + paramLength;

            if (available < packetLength)
            {
                break;
            }

            ReadOnlySpan<byte> packet = buffer.AsSpan(index, packetLength);
            if (!ValidateChecksum(packet))
            {
                Log?.Invoke("[WARN] Checksum mismatch, skipping packet candidate.");
                index += packetLength;
                continue;
            }

            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(packetLength);
            packet.CopyTo(rentedBuffer);

            if (!writer.TryWrite(new RadarFrame(rentedBuffer, packetLength)))
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
                Log?.Invoke("[WARN] Frame enqueue failed.");
            }

            index += packetLength;
        }

        if (index > 0)
        {
            int remaining = bufferedCount - index;
            if (remaining > 0)
            {
                Buffer.BlockCopy(buffer, index, buffer, 0, remaining);
            }

            bufferedCount = remaining;
        }
    }

    /// <summary>将单个已验证帧分发到对应的事件处理。</summary>
    private void DispatchPacket(ReadOnlySpan<byte> packet)
    {
        byte cmd = packet[4];
        switch (cmd)
        {
            case AckCommand:
                if (packet.Length < 10)
                {
                    return;
                }

                var ack = new RadarAck(packet[2], packet[3], packet[7], packet[8]);
                AckReceived?.Invoke(ack);

                if (ack.AcknowledgedCommand == StatusReadCommand)
                {
                    ushort paramLength = BinaryPrimitives.ReadUInt16LittleEndian(packet.Slice(5, 2));
                    if (paramLength > 2)
                    {
                        byte[] rawStatus = packet.Slice(9, packet.Length - 10).ToArray();
                        StatusReceived?.Invoke(new RadarStatus { Raw = rawStatus });
                    }
                }
                break;

            case StatusReportCommand:
            case StatusReadCommand:
                byte[] statusPayload = packet.Slice(7, packet.Length - 8).ToArray();
                StatusReceived?.Invoke(new RadarStatus { Raw = statusPayload });
                break;

            case TargetUploadCommand:
            case TargetUploadCommandA9:
                RadarTargetPacket targetPacket = ParseTargetPacket(packet);
                TargetPacketReceived?.Invoke(targetPacket);
                TargetUploadPacketReceived?.Invoke(packet.Length);
                TargetUploadPacketDetected?.Invoke(cmd, packet.Length);

                // 调试：打印第1个目标的原始 hex（验证 Z/Elevation 偏移量是否正确）
                if (targetPacket.Targets.Count > 0)
                {
                    int tds = cmd == TargetUploadCommandA9 ? 11 : 8;
                    if (packet.Length >= tds + 68)
                    {
                        var raw = packet.Slice(tds, 68);
                        Console.WriteLine(
                            $"[RAW TARGET hex:{tds}] Z(28-31)={raw[28]:X2}{raw[29]:X2}{raw[30]:X2}{raw[31]:X2} " +
                            $"Elev(40-43)={raw[40]:X2}{raw[41]:X2}{raw[42]:X2}{raw[43]:X2} " +
                            $"X(20-23)={raw[20]:X2}{raw[21]:X2}{raw[22]:X2}{raw[23]:X2} " +
                            $"Y(24-27)={raw[24]:X2}{raw[25]:X2}{raw[26]:X2}{raw[27]:X2} " +
                            $"Area(52-53)={raw[52]:X2}{raw[53]:X2}");
                    }
                }
                break;

            default:
                Log?.Invoke($"[RX] Unhandled cmd=0x{cmd:X2}, len={packet.Length}");
                break;
        }
    }

    /// <summary>
    /// 解析 A8 / A9 目标上传包。
    /// - A8：参数区第 1 字节是目标数（1字节）
    /// - A9：参数区前 4 字节是目标数（uint32 小端）
    /// </summary>
    private static RadarTargetPacket ParseTargetPacket(ReadOnlySpan<byte> packet)
    {
        ushort paramLength = BinaryPrimitives.ReadUInt16LittleEndian(packet.Slice(5, 2));

        uint targetCount;
        int targetDataStart;

        if (packet[4] == TargetUploadCommandA9)
        {
            // A9：参数区开头 4 字节为目标数（uint32 小端）
            if (paramLength < 4)
            {
                targetCount = 0;
                targetDataStart = 8;
            }
            else
            {
                targetCount = BinaryPrimitives.ReadUInt32LittleEndian(packet.Slice(7, 4));
                targetDataStart = 11;
            }
        }
        else
        {
            // A8：参数区开头 1 字节为目标数
            targetCount = packet[7];
            targetDataStart = 8;
        }

        int targetDataAvailable = packet.Length - 1 - targetDataStart;
        int maxTargetsByData = targetDataAvailable / TargetBlockSize;
        int parseCount = Math.Min((int)Math.Min(targetCount, int.MaxValue), maxTargetsByData);

        var targets = new List<RadarTarget>(parseCount);
        for (int i = 0; i < parseCount; i++)
        {
            int offset = targetDataStart + i * TargetBlockSize;
            targets.Add(ParseSingleTarget(packet, offset));
        }

        return new RadarTargetPacket
        {
            Command = packet[4],
            SourceAddress = packet[2],
            DestinationAddress = packet[3],
            ParamLength = paramLength,
            TargetCount = targetCount,
            Targets = targets
        };
    }

    /// <summary>
    /// 解析单个目标结构。
    /// 单目标固定占 68 字节，字段顺序与 A8 / A9 协议文档一致。
    /// </summary>
    private static RadarTarget ParseSingleTarget(ReadOnlySpan<byte> packet, int offset)
    {
        return new RadarTarget
        {
            Id = BinaryPrimitives.ReadUInt32LittleEndian(packet.Slice(offset + 0, 4)),
            Type = BinaryPrimitives.ReadUInt32LittleEndian(packet.Slice(offset + 4, 4)),
            XSpeed = ReadSingleLittleEndian(packet, offset + 8),
            YSpeed = ReadSingleLittleEndian(packet, offset + 12),
            ZSpeed = ReadSingleLittleEndian(packet, offset + 16),
            XAxis = ReadSingleLittleEndian(packet, offset + 20),
            YAxis = ReadSingleLittleEndian(packet, offset + 24),
            ZAxis = ReadSingleLittleEndian(packet, offset + 28),
            Length = ReadSingleLittleEndian(packet, offset + 32),
            AzimuthAngle = ReadSingleLittleEndian(packet, offset + 36),
            ElevationAngle = ReadSingleLittleEndian(packet, offset + 40),
            Snr = ReadSingleLittleEndian(packet, offset + 44),
            PeakEnergy = ReadSingleLittleEndian(packet, offset + 48),
            Area = BinaryPrimitives.ReadUInt16LittleEndian(packet.Slice(offset + 52, 2)),
            Reserved = packet.Slice(offset + 54, 14).ToArray()
        };
    }

    /// <summary>在提供的 Span 中查找帧头 0xA5 0x5A，返回索引或 -1。</summary>
    private static int FindHeader(ReadOnlySpan<byte> data)
    {
        for (int i = 0; i < data.Length - 1; i++)
        {
            if (data[i] == StartCode0 && data[i + 1] == StartCode1)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 验证校验和。支持两种常见变体：
    /// - 从字节 0 累加到校验和前一字节
    /// - 从字节 2（不含起始码）累加到校验和前一字节
    /// 设备文档对是否包含起始码描述不统一，兼容两种实现能提高健壮性。
    /// </summary>
    private static bool ValidateChecksum(ReadOnlySpan<byte> packet)
    {
        byte checksum = packet[^1];
        byte sumAll = CalcChecksum(packet, 0);
        byte sumWithoutStart = CalcChecksum(packet, 2);
        return checksum == sumAll || checksum == sumWithoutStart;
    }

    /// <summary>从 startIndex 开始累加到校验和前一字节（不包含校验和字节），并返回低 8 位。</summary>
    private static byte CalcChecksum(ReadOnlySpan<byte> packet, int startIndex)
    {
        int sum = 0;
        for (int i = startIndex; i < packet.Length - 1; i++)
        {
            sum += packet[i];
        }

        return (byte)(sum & 0xFF);
    }

    /// <summary>按小端方式读取 4 字节 IEEE 754 单精度浮点数。</summary>
    private static float ReadSingleLittleEndian(ReadOnlySpan<byte> data, int offset)
    {
        int bits = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset, 4));
        return BitConverter.Int32BitsToSingle(bits);
    }
}
