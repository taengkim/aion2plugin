namespace AionPlugin.Models;

/// <summary>
/// 파싱된 데미지 이벤트 1건
/// 실제 아이온2 패킷 구조 기반 (AION2Meter4J 참조)
/// </summary>
public record DamageEvent
{
    /// <summary>공격자 동적 EntityId (VarInt)</summary>
    public int ActorId { get; init; }

    /// <summary>피격자 동적 EntityId (VarInt)</summary>
    public int TargetId { get; init; }

    /// <summary>스킬 코드 (UInt32LE, DoT는 /100)</summary>
    public int SkillCode { get; init; }

    /// <summary>데미지 값 (VarInt)</summary>
    public int Damage { get; init; }

    /// <summary>타입 (3 = 크리티컬)</summary>
    public int Type { get; init; }

    /// <summary>크리티컬 여부</summary>
    public bool IsCritical => Type == 3;

    /// <summary>DoT (지속 데미지) 여부</summary>
    public bool IsDot { get; init; }

    /// <summary>특수 플래그</summary>
    public SpecialDamage Specials { get; init; }

    /// <summary>switch 변수 (패킷 구조 분기용)</summary>
    public int SwitchVariable { get; init; }

    /// <summary>이벤트 수신 시각 (UTC)</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
