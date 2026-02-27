namespace AionPlugin.Models;

/// <summary>
/// 특수 데미지 플래그 (패킷의 specials 블록 첫 바이트 비트 필드)
/// </summary>
[Flags]
public enum SpecialDamage
{
    None        = 0x00,
    Back        = 0x01,  // 등 공격
    Unknown     = 0x02,
    Parry       = 0x04,  // 막기
    Perfect     = 0x08,  // 퍼펙트
    Double      = 0x10,  // 이중 타격
    Endure      = 0x20,  // 버팀
    Unknown4    = 0x40,
    PowerShard  = 0x80   // 파워 샤드
}
