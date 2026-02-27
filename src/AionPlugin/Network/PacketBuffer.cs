namespace AionPlugin.Network;

/// <summary>
/// 아이온2 TCP 스트림을 완성된 패킷 단위로 조립합니다.
///
/// 프레이밍 규칙 (AION2Meter4J 기반):
///   - 패킷 끝에 매직 트레일러 0x06 0x00 0x36 이 붙음
///   - StreamAssembler 와 동일하게, 트레일러 앞 데이터가 하나의 패킷
///   - VarInt 로 인코딩된 길이 필드가 패킷 맨 앞에 있음 (자기 자신 포함)
///   - 파서에는 트레일러 3 바이트를 제거한 데이터가 전달됨
/// </summary>
public class PacketBuffer
{
    // 패킷 끝을 알리는 매직 트레일러
    private static readonly byte[] MagicTrailer = [0x06, 0x00, 0x36];

    // 링 버퍼 (10 MB, PacketAccumulator 동일 크기)
    private const int Capacity = 10 * 1024 * 1024;
    private readonly byte[] _ring = new byte[Capacity];
    private int _readPos;
    private int _writePos;
    private int _count;

    /// <summary>
    /// TCP 청크를 누적하고, 완성된 패킷을 반환합니다.
    /// </summary>
    public IEnumerable<byte[]> Feed(byte[] data)
    {
        Append(data);

        while (true)
        {
            int idx = IndexOf(MagicTrailer);
            if (idx == -1) yield break;   // 트레일러가 아직 없음 = 더 기다림

            // 트레일러 포함 길이
            int packetLen = idx + MagicTrailer.Length;
            byte[] raw = ReadAndDiscard(packetLen);

            // 트레일러 3바이트 제거 후 전달
            if (raw.Length > MagicTrailer.Length)
                yield return raw[..^MagicTrailer.Length];
        }
    }

    public void Reset()
    {
        _readPos = 0;
        _writePos = 0;
        _count = 0;
    }

    // ──────────────────────────────────────────────────
    //  링 버퍼 내부 구현
    // ──────────────────────────────────────────────────

    private void Append(byte[] data)
    {
        if (_count + data.Length > Capacity)
        {
            Reset();  // 오버플로우 시 초기화 (PacketAccumulator 동일 처리)
            return;
        }

        int spaceToEnd = Capacity - _writePos;
        int copyLen = Math.Min(data.Length, spaceToEnd);
        Array.Copy(data, 0, _ring, _writePos, copyLen);

        if (data.Length > copyLen)
            Array.Copy(data, copyLen, _ring, 0, data.Length - copyLen);

        _writePos = (_writePos + data.Length) % Capacity;
        _count += data.Length;
    }

    private int IndexOf(byte[] pattern)
    {
        if (_count < pattern.Length) return -1;

        for (int i = 0; i <= _count - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (_ring[(_readPos + i + j) % Capacity] != pattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return -1;
    }

    private byte[] ReadAndDiscard(int length)
    {
        if (length <= 0 || length > _count) return [];

        var result = new byte[length];
        int spaceToEnd = Capacity - _readPos;
        int copyLen = Math.Min(length, spaceToEnd);

        Array.Copy(_ring, _readPos, result, 0, copyLen);
        if (length > copyLen)
            Array.Copy(_ring, 0, result, copyLen, length - copyLen);

        _readPos = (_readPos + length) % Capacity;
        _count -= length;
        return result;
    }
}
