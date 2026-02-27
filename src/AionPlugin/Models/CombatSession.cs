namespace AionPlugin.Models;

/// <summary>
/// 전투 세션 (파티 전투 1회)
/// </summary>
public class CombatSession
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public bool IsActive => EndTime is null;

    public TimeSpan Duration => IsActive
        ? DateTime.UtcNow - StartTime
        : EndTime!.Value - StartTime;

    public double ElapsedSeconds => Math.Max(Duration.TotalSeconds, 0.001);

    public List<DamageEvent> Events { get; } = new();

    /// <summary>세션 내 전체 데미지 합산</summary>
    public long TotalDamage => Events
        .Where(e => e.Damage > 0)
        .Sum(e => (long)e.Damage);

    /// <summary>공격자(ActorId)별 데미지 집계</summary>
    public IEnumerable<(int ActorId, long Damage)> DamageByAttacker()
        => Events
            .Where(e => e.Damage > 0)
            .GroupBy(e => e.ActorId)
            .Select(g => (g.Key, g.Sum(e => (long)e.Damage)));
}
