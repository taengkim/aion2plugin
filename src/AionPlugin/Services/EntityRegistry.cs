using AionPlugin.Models;
using System.Collections.Concurrent;

namespace AionPlugin.Services;

/// <summary>
/// 세션 동안 관찰된 플레이어 엔티티를 EntityId 기준으로 관리합니다.
/// DataStorage.kt 의 entityMap 역할을 담당합니다.
///
/// 아이온2에서 EntityId 는 세션마다 재사용되는 동적 ID 이므로,
/// 닉네임 패킷이 도착한 뒤에야 실제 이름을 알 수 있습니다.
/// </summary>
public class EntityRegistry
{
    private readonly ConcurrentDictionary<int, Player> _players = new();

    /// <summary>
    /// 데미지 이벤트에서 처음 관찰된 EntityId 를 임시 이름으로 등록합니다.
    /// </summary>
    public Player GetOrCreate(int entityId)
    {
        return _players.GetOrAdd(entityId, id => new Player
        {
            EntityId  = id,
            Name      = $"User_{id}",
            ClassName = string.Empty
        });
    }

    /// <summary>
    /// 닉네임 패킷 수신 시 이름을 업데이트합니다.
    /// (hasPossibilityNickname 통과 후 호출)
    /// </summary>
    public void UpdateNickname(int entityId, string name)
    {
        var player = _players.GetOrAdd(entityId, id => new Player
        {
            EntityId = id,
            Name     = name
        });

        // 이미 확정된 이름(3글자 이상)을 짧은 이름으로 덮어쓰지 않음
        if (player.Name.Length >= 3 && name.Length <= 2) return;
        if (player.Name == name) return;

        player.Name = name;
    }

    public Player? Find(int entityId) => _players.GetValueOrDefault(entityId);

    public IEnumerable<Player> All => _players.Values;

    public void Clear() => _players.Clear();
}
