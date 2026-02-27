namespace AionPlugin.Models;

/// <summary>
/// 단일 데미지 이벤트 (패킷 1건 = 1 이벤트)
/// </summary>
public record DamageEvent
{
    /// <summary>공격자 ObjectId</summary>
    public uint AttackerId { get; init; }

    /// <summary>피격자 ObjectId</summary>
    public uint TargetId { get; init; }

    /// <summary>스킬 ID</summary>
    public uint SkillId { get; init; }

    /// <summary>데미지 값 (음수 = 힐)</summary>
    public long Damage { get; init; }

    /// <summary>크리티컬 여부</summary>
    public bool IsCritical { get; init; }

    /// <summary>이벤트 발생 시각</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>데미지 유형</summary>
    public DamageType Type { get; init; } = DamageType.Physical;
}

public enum DamageType
{
    Physical,
    Magical,
    True,   // 방어 무시
    Heal
}
