using AionPlugin.Models;
using System.Buffers.Binary;
using System.Text;

namespace AionPlugin.Network;

/// <summary>
/// 아이온2 게임 패킷을 파싱해 게임 이벤트를 생성합니다.
///
/// ※ 주의: 아이온2의 실제 패킷 구조는 역공학으로 파악해야 합니다.
///   아래 OpCode 와 오프셋은 커뮤니티 분석 자료를 바탕으로 한 추정값이며,
///   실제 게임과 다를 수 있습니다. 패킷 스니핑 결과에 맞게 업데이트하세요.
/// </summary>
public class PacketParser
{
    // ──────────────────────────────────────────────────
    //  서버 → 클라이언트 OpCode (추정값, 업데이트 필요)
    // ──────────────────────────────────────────────────
    private const ushort OP_ATTACK_RESULT   = 0x0075;  // 공격 결과
    private const ushort OP_SKILL_RESULT    = 0x0076;  // 스킬 결과
    private const ushort OP_PLAYER_INFO     = 0x0021;  // 플레이어 정보
    private const ushort OP_PARTY_INFO      = 0x0097;  // 파티원 목록
    private const ushort OP_PLAYER_SPAWN    = 0x0011;  // 플레이어 입장
    private const ushort OP_PLAYER_DESPAWN  = 0x0012;  // 플레이어 퇴장
    private const ushort OP_COMBAT_START    = 0x01A0;  // 전투 시작
    private const ushort OP_COMBAT_END      = 0x01A1;  // 전투 종료

    public event Action<DamageEvent>? DamageEventParsed;
    public event Action<Player>? PlayerInfoParsed;
    public event Action<List<Player>>? PartyInfoParsed;
    public event Action? CombatStarted;
    public event Action? CombatEnded;

    private readonly PacketBuffer _serverBuffer = new();
    private readonly PacketBuffer _clientBuffer = new();

    /// <summary>
    /// 원시 패킷 데이터를 파싱합니다.
    /// </summary>
    public void Feed(RawPacketData raw)
    {
        var buffer = raw.IsFromServer ? _serverBuffer : _clientBuffer;
        foreach (var packet in buffer.Feed(raw.Data))
        {
            if (raw.IsFromServer)
                ParseServerPacket(packet);
        }
    }

    private void ParseServerPacket(Aion2Packet packet)
    {
        try
        {
            switch (packet.OpCode)
            {
                case OP_ATTACK_RESULT:
                case OP_SKILL_RESULT:
                    ParseDamagePacket(packet);
                    break;
                case OP_PLAYER_INFO:
                    ParsePlayerInfo(packet);
                    break;
                case OP_PARTY_INFO:
                    ParsePartyInfo(packet);
                    break;
                case OP_COMBAT_START:
                    CombatStarted?.Invoke();
                    break;
                case OP_COMBAT_END:
                    CombatEnded?.Invoke();
                    break;
            }
        }
        catch
        {
            // 파싱 실패 시 해당 패킷 무시
        }
    }

    /// <summary>
    /// 데미지 패킷 파싱
    /// 실제 오프셋은 패킷 스니핑 후 업데이트 필요
    /// </summary>
    private void ParseDamagePacket(Aion2Packet packet)
    {
        var data = packet.Payload;
        if (data.Length < 20) return;

        int offset = 0;

        uint attackerId = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
        offset += 4;

        uint targetId = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
        offset += 4;

        uint skillId = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
        offset += 4;

        int damage = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, 4));
        offset += 4;

        byte flags = data[offset];
        bool isCritical = (flags & 0x01) != 0;
        bool isMagical  = (flags & 0x02) != 0;

        if (damage == 0) return;

        DamageEventParsed?.Invoke(new DamageEvent
        {
            AttackerId = attackerId,
            TargetId   = targetId,
            SkillId    = skillId,
            Damage     = damage,
            IsCritical = isCritical,
            Type       = isMagical ? DamageType.Magical : DamageType.Physical,
            Timestamp  = packet.Timestamp
        });
    }

    /// <summary>
    /// 플레이어 정보 패킷 파싱
    /// </summary>
    private void ParsePlayerInfo(Aion2Packet packet)
    {
        var data = packet.Payload;
        if (data.Length < 12) return;

        int offset = 0;

        uint objectId = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
        offset += 4;

        // 이름 길이 (2 bytes) + 이름 (UTF-16LE)
        ushort nameLen = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2));
        offset += 2;

        if (data.Length < offset + nameLen * 2 + 2) return;

        string name = Encoding.Unicode.GetString(data, offset, nameLen * 2);
        offset += nameLen * 2;

        byte classId = data[offset];
        string className = ClassIdToName(classId);

        PlayerInfoParsed?.Invoke(new Player
        {
            ObjectId  = objectId,
            Name      = name,
            ClassName = className
        });
    }

    /// <summary>
    /// 파티원 목록 패킷 파싱
    /// </summary>
    private void ParsePartyInfo(Aion2Packet packet)
    {
        var data = packet.Payload;
        if (data.Length < 2) return;

        var players = new List<Player>();
        int offset = 0;

        byte memberCount = data[offset++];

        for (int i = 0; i < memberCount && offset + 8 <= data.Length; i++)
        {
            uint objectId = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
            offset += 4;

            ushort nameLen = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2));
            offset += 2;

            if (offset + nameLen * 2 > data.Length) break;

            string name = Encoding.Unicode.GetString(data, offset, nameLen * 2);
            offset += nameLen * 2;

            if (offset >= data.Length) break;
            byte classId = data[offset++];

            players.Add(new Player
            {
                ObjectId  = objectId,
                Name      = name,
                ClassName = ClassIdToName(classId)
            });
        }

        if (players.Count > 0)
            PartyInfoParsed?.Invoke(players);
    }

    private static string ClassIdToName(byte id) => id switch
    {
        1  => "Warrior",
        2  => "Assassin",
        3  => "Ranger",
        4  => "Mage",
        5  => "Healer",
        6  => "Summoner",
        7  => "Chanter",
        _  => $"Class{id}"
    };
}
