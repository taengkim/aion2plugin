using AionPlugin.Models;
using System.Text;

namespace AionPlugin.Network;

/// <summary>
/// 아이온2 서버→클라이언트 패킷을 파싱합니다.
///
/// 패킷 구조 분석 출처: AION2Meter4J (ion2me/Aion2Meter_public)
///
/// 핵심 규칙:
///   - 모든 숫자 필드는 VarInt (7-bit 가변 길이) 인코딩
///   - 패킷 = [VarInt 전체길이] [2바이트 Opcode] [필드들...] [트레일러 06 00 36]
///   - StreamProcessor 에서 트레일러를 제거한 뒤 이 클래스로 전달됨
/// </summary>
public class PacketParser
{
    // ──────────────────────────────────────────────────
    //  OpCode (2 바이트, big-endian 표현)
    // ──────────────────────────────────────────────────
    // 일반 데미지:  첫 바이트=0x04, 둘째=0x38
    // DoT 데미지:   첫 바이트=0x05, 둘째=0x38
    // 닉네임:       첫 바이트=0x04, 둘째=0x8D
    // 맵 ID:        첫 바이트=0x00, 둘째=0x61
    // 소환/스폰:    첫 바이트=0x40, 둘째=0x36
    private const byte OP_DAMAGE_B0      = 0x04;
    private const byte OP_DAMAGE_B1      = 0x38;
    private const byte OP_DOT_B0         = 0x05;
    private const byte OP_DOT_B1         = 0x38;
    private const byte OP_NICKNAME_B0    = 0x04;
    private const byte OP_NICKNAME_B1    = 0x8D;

    private const int  FLAG_MASK = 0x0F;  // switchVariable 하위 4비트

    // ──────────────────────────────────────────────────
    //  이벤트
    // ──────────────────────────────────────────────────
    public event Action<DamageEvent>? DamageEventParsed;
    public event Action<(int EntityId, string Name)>? NicknameParsed;

    private readonly PacketBuffer _buffer = new();

    // ──────────────────────────────────────────────────
    //  진입점
    // ──────────────────────────────────────────────────

    /// <summary>서버에서 수신한 TCP 청크를 공급합니다.</summary>
    public void Feed(byte[] tcpChunk)
    {
        foreach (var packet in _buffer.Feed(tcpChunk))
            ParsePacket(packet);
    }

    // ──────────────────────────────────────────────────
    //  패킷 라우팅 (StreamProcessor.parsePerfectPacket 동일 순서)
    // ──────────────────────────────────────────────────

    private void ParsePacket(byte[] packet)
    {
        if (packet.Length < 3) return;

        try
        {
            // 1. 전체 길이가 VarInt 길이 필드와 일치하면 정상 패킷
            var (len, lenSize) = ReadVarInt(packet, 0);
            if (lenSize < 0) return;

            int offset = lenSize;
            if (offset + 2 > packet.Length) return;

            byte b0 = packet[offset];
            byte b1 = packet[offset + 1];

            // 첫 바이트가 0x20 이면 데미지 포맷 아님 (StreamProcessor 동일 처리)
            if (b0 == 0x20) return;

            // 일반 데미지
            if (b0 == OP_DAMAGE_B0 && b1 == OP_DAMAGE_B1)
            {
                ParseDamage(packet, offset + 2, isDot: false);
                return;
            }

            // DoT 데미지
            if (b0 == OP_DOT_B0 && b1 == OP_DOT_B1)
            {
                ParseDamage(packet, offset + 2, isDot: true);
                return;
            }

            // 닉네임
            if (b0 == OP_NICKNAME_B0 && b1 == OP_NICKNAME_B1)
            {
                ParseNickname(packet, offset + 2);
            }
        }
        catch
        {
            // 파싱 실패 패킷은 무시
        }
    }

    // ──────────────────────────────────────────────────
    //  일반 데미지 파싱  (StreamProcessor.parsingDamage)
    //
    //  필드 순서:
    //    [VarInt targetId]
    //    [VarInt switchVariable]
    //    [VarInt flag]
    //    [VarInt actorId]
    //    [UInt32LE skillCode] [1 byte skip]
    //    [VarInt type]
    //    [Special block: (switch & 0x0F) → 4→8 B, 5→12 B, 6→10 B, 7→14 B]
    //    [VarInt unknown]
    //    [VarInt damage]
    //    [VarInt loop]
    // ──────────────────────────────────────────────────
    private void ParseDamage(byte[] p, int offset, bool isDot)
    {
        if (isDot)
        {
            ParseDoT(p, offset);
            return;
        }

        // targetId
        var (targetId, tSz) = ReadVarInt(p, offset); if (tSz < 0) return;
        offset += tSz;

        // switchVariable
        var (sw, swSz) = ReadVarInt(p, offset); if (swSz < 0) return;
        offset += swSz;

        // flag
        var (flag, flSz) = ReadVarInt(p, offset); if (flSz < 0) return;
        offset += flSz;

        // actorId
        var (actorId, aSz) = ReadVarInt(p, offset); if (aSz < 0) return;
        offset += aSz;

        // skillCode (UInt32LE) + 1 byte skip
        if (offset + 5 > p.Length) return;
        int skillCode = ReadUInt32LE(p, offset);
        offset += 5;   // 4 + 1 skip

        // type
        var (type, tpSz) = ReadVarInt(p, offset); if (tpSz < 0) return;
        offset += tpSz;

        // special 블록 크기 결정 (switch & 0x0F)
        int specialsLen = (sw & FLAG_MASK) switch
        {
            4 => 8,
            5 => 12,
            6 => 10,
            7 => 14,
            _ => -1
        };
        if (specialsLen < 0) return;

        // specials
        SpecialDamage specials = SpecialDamage.None;
        if (specialsLen > 0 && offset + specialsLen <= p.Length)
        {
            if (specialsLen != 8)  // 8 바이트 블록은 플래그 없음 (원본 동일)
                specials = (SpecialDamage)(p[offset] & 0xFF);
        }
        offset += specialsLen;
        if (offset > p.Length) return;

        // unknown
        var (_, uSz) = ReadVarInt(p, offset); if (uSz < 0) return;
        offset += uSz;

        // damage
        var (damage, dSz) = ReadVarInt(p, offset); if (dSz < 0) return;
        offset += dSz;

        // 자해 제외 (actorId == targetId)
        if (actorId == targetId || damage == 0) return;

        DamageEventParsed?.Invoke(new DamageEvent
        {
            ActorId        = actorId,
            TargetId       = targetId,
            SkillCode      = skillCode,
            Damage         = damage,
            Type           = type,
            Specials       = specials,
            SwitchVariable = sw,
            IsDot          = false,
            Timestamp      = DateTime.UtcNow
        });
    }

    // ──────────────────────────────────────────────────
    //  DoT 데미지 파싱  (StreamProcessor.parseDoTPacket)
    //
    //  필드 순서:
    //    [VarInt targetId]
    //    [1 byte skip]
    //    [VarInt actorId]
    //    [VarInt unknown]
    //    [UInt32LE skillCode / 100]
    //    [VarInt damage]
    // ──────────────────────────────────────────────────
    private void ParseDoT(byte[] p, int offset)
    {
        var (targetId, tSz) = ReadVarInt(p, offset); if (tSz < 0) return;
        offset += tSz + 1;   // +1 skip

        var (actorId, aSz) = ReadVarInt(p, offset); if (aSz < 0) return;
        if (actorId == targetId) return;
        offset += aSz;

        var (_, uSz) = ReadVarInt(p, offset); if (uSz < 0) return;
        offset += uSz;

        if (offset + 4 > p.Length) return;
        int skillCode = ReadUInt32LE(p, offset) / 100;
        offset += 4;

        var (damage, dSz) = ReadVarInt(p, offset); if (dSz < 0) return;

        if (damage == 0) return;

        DamageEventParsed?.Invoke(new DamageEvent
        {
            ActorId   = actorId,
            TargetId  = targetId,
            SkillCode = skillCode,
            Damage    = damage,
            IsDot     = true,
            Timestamp = DateTime.UtcNow
        });
    }

    // ──────────────────────────────────────────────────
    //  닉네임 파싱  (StreamProcessor.parsingNickname)
    //
    //  offset = 2 (opcode 이미 소비) 기준
    //  절대 offset 10 부터 VarInt entityId, 그 다음 1 byte 이름 길이, UTF-8 이름
    // ──────────────────────────────────────────────────
    private void ParseNickname(byte[] p, int opcodeBodyStart)
    {
        // 닉네임 패킷: opcode 2바이트 이미 소비, VarInt 길이 앞에 있었음
        // StreamProcessor 는 offset=10 에서 VarInt entityId 를 읽음
        // (VarInt 길이 크기가 다를 수 있어 고정 10을 씀 - 원본과 동일)
        int offset = 10;
        if (offset >= p.Length) return;

        var (entityId, eSz) = ReadVarInt(p, offset);
        if (eSz <= 0) return;
        offset += eSz;

        if (offset >= p.Length) return;
        int nameLen = p[offset] & 0xFF;
        if (nameLen <= 0 || nameLen > 72) return;
        offset++;

        if (offset + nameLen > p.Length) return;
        string name = Encoding.UTF8.GetString(p, offset, nameLen);

        if (IsValidNickname(name))
            NicknameParsed?.Invoke((entityId, name));
    }

    // ──────────────────────────────────────────────────
    //  유틸
    // ──────────────────────────────────────────────────

    /// <summary>
    /// VarInt 디코딩 (StreamProcessor.readVarInt 동일 로직)
    /// 반환: (값, 소비한 바이트 수). 오류 시 (-1, -1).
    /// </summary>
    public static (int value, int size) ReadVarInt(byte[] bytes, int offset)
    {
        int value = 0, shift = 0, count = 0;
        while (true)
        {
            if (offset + count >= bytes.Length) return (-1, -1);
            int b = bytes[offset + count] & 0xFF;
            count++;
            value |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0) return (value, count);
            shift += 7;
            if (shift >= 32) return (-1, -1);
        }
    }

    private static int ReadUInt32LE(byte[] p, int offset)
    {
        return (p[offset] & 0xFF)
             | ((p[offset + 1] & 0xFF) << 8)
             | ((p[offset + 2] & 0xFF) << 16)
             | ((p[offset + 3] & 0xFF) << 24);
    }

    /// <summary>
    /// 닉네임 유효성 검사 (hasPossibilityNickname 동일)
    /// 한글/영숫자 혼합, 순수 숫자 제외, 1글자 영문 제외
    /// </summary>
    private static bool IsValidNickname(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[가-힣a-zA-Z0-9]+$")) return false;
        if (System.Text.RegularExpressions.Regex.IsMatch(name, @"^[0-9]+$")) return false;
        if (System.Text.RegularExpressions.Regex.IsMatch(name, @"^[A-Za-z]$")) return false;
        return true;
    }
}
