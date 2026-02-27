using System.Buffers.Binary;

namespace AionPlugin.Network;

/// <summary>
/// TCP 스트림에서 아이온2 패킷 경계를 복원합니다.
/// 아이온2는 패킷 헤더에 길이 필드가 있습니다.
/// </summary>
public class PacketBuffer
{
    // 헤더 레이아웃: [Length: 2 bytes LE] [OpCode: 2 bytes LE] [Data: N bytes]
    private const int HeaderSize = 4;

    private readonly List<byte> _buffer = new(4096);

    /// <summary>
    /// 원시 TCP 데이터를 버퍼에 추가하고 완성된 패킷들을 꺼냅니다.
    /// </summary>
    public IEnumerable<Aion2Packet> Feed(byte[] data)
    {
        _buffer.AddRange(data);

        while (_buffer.Count >= HeaderSize)
        {
            // 패킷 길이 (헤더 포함)
            ushort packetLength = BinaryPrimitives.ReadUInt16LittleEndian(
                _buffer.Take(2).ToArray());

            if (packetLength < HeaderSize || packetLength > 65535)
            {
                // 잘못된 패킷 → 스트림 재동기화
                _buffer.Clear();
                yield break;
            }

            if (_buffer.Count < packetLength)
                yield break; // 아직 데이터가 부족

            ushort opCode = BinaryPrimitives.ReadUInt16LittleEndian(
                _buffer.Skip(2).Take(2).ToArray());

            byte[] payload = _buffer.Skip(HeaderSize)
                                    .Take(packetLength - HeaderSize)
                                    .ToArray();

            _buffer.RemoveRange(0, packetLength);

            yield return new Aion2Packet
            {
                Length = packetLength,
                OpCode = opCode,
                Payload = payload,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public void Reset() => _buffer.Clear();
}

/// <summary>
/// 파싱된 아이온2 패킷 한 개
/// </summary>
public class Aion2Packet
{
    public ushort Length { get; init; }
    public ushort OpCode { get; init; }
    public byte[] Payload { get; init; } = Array.Empty<byte>();
    public DateTime Timestamp { get; init; }

    public override string ToString() => $"[0x{OpCode:X4}] len={Length}";
}
