using AionPlugin.Models;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace AionPlugin.Services;

/// <summary>
/// 파티원 DPS를 추적하고 실시간 랭킹을 유지합니다.
/// </summary>
public class DpsTracker : IDisposable
{
    private CombatSession? _currentSession;
    private readonly Dictionary<uint, Player> _players = new();
    private readonly DispatcherTimer _updateTimer;
    private bool _isInCombat;

    /// <summary>
    /// UI 바인딩용 정렬된 플레이어 목록 (DPS 내림차순)
    /// </summary>
    public ObservableCollection<Player> SortedPlayers { get; } = new();

    public bool IsInCombat => _isInCombat;
    public CombatSession? CurrentSession => _currentSession;

    public event Action? SessionStarted;
    public event Action? SessionEnded;
    public event Action? DataUpdated;

    public DpsTracker()
    {
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500) // 0.5초마다 UI 갱신
        };
        _updateTimer.Tick += (_, _) => UpdateDisplayValues();
    }

    // ──────────────────────────────────────────────────
    //  파티원 등록
    // ──────────────────────────────────────────────────

    public void RegisterPlayer(Player player)
    {
        if (_players.ContainsKey(player.ObjectId))
        {
            // 이름/직업 업데이트
            var existing = _players[player.ObjectId];
            existing.Name = player.Name;
            existing.ClassName = player.ClassName;
        }
        else
        {
            _players[player.ObjectId] = player;
        }
    }

    public void RegisterParty(IEnumerable<Player> players)
    {
        foreach (var p in players)
            RegisterPlayer(p);
    }

    // ──────────────────────────────────────────────────
    //  전투 세션 제어
    // ──────────────────────────────────────────────────

    public void StartSession()
    {
        _currentSession = new CombatSession { StartTime = DateTime.UtcNow };
        _isInCombat = true;
        _updateTimer.Start();
        SessionStarted?.Invoke();
    }

    public void EndSession()
    {
        if (_currentSession is null) return;

        _currentSession.EndTime = DateTime.UtcNow;
        _isInCombat = false;
        _updateTimer.Stop();
        UpdateDisplayValues(isFinal: true);
        SessionEnded?.Invoke();
    }

    public void ResetSession()
    {
        _currentSession = null;
        _isInCombat = false;
        _updateTimer.Stop();

        foreach (var p in _players.Values)
        {
            p.TotalDamage = 0;
            p.Dps = 0;
            p.DamagePercent = 0;
        }

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
            SortedPlayers.Clear());
    }

    // ──────────────────────────────────────────────────
    //  데미지 이벤트 처리
    // ──────────────────────────────────────────────────

    public void OnDamageEvent(DamageEvent evt)
    {
        // 치유는 DPS 집계에서 제외 (필요 시 힐러 별도 집계 가능)
        if (evt.Damage <= 0) return;

        // 전투 중이 아니면 자동 시작
        if (!_isInCombat)
            StartSession();

        _currentSession?.Events.Add(evt);

        // 알려지지 않은 공격자 → 임시 등록
        if (!_players.TryGetValue(evt.AttackerId, out _))
        {
            _players[evt.AttackerId] = new Player
            {
                ObjectId = evt.AttackerId,
                Name = $"Unknown({evt.AttackerId})",
                ClassName = "Unknown"
            };
        }
    }

    // ──────────────────────────────────────────────────
    //  UI 값 계산 & 갱신
    // ──────────────────────────────────────────────────

    private void UpdateDisplayValues(bool isFinal = false)
    {
        if (_currentSession is null) return;

        double elapsed = _currentSession.ElapsedSeconds;
        var damageMap = _currentSession.DamageByAttacker()
            .ToDictionary(x => x.AttackerId, x => x.Damage);

        long partyTotal = damageMap.Values.Sum();

        foreach (var (id, dmg) in damageMap)
        {
            if (!_players.TryGetValue(id, out var player)) continue;

            player.TotalDamage = dmg;
            player.Dps = dmg / elapsed;
            player.DamagePercent = partyTotal > 0
                ? (double)dmg / partyTotal * 100.0
                : 0;
        }

        // 정렬 갱신 (UI 스레드에서)
        var sorted = _players.Values
            .Where(p => p.TotalDamage > 0)
            .OrderByDescending(p => p.TotalDamage)
            .ToList();

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            SortedPlayers.Clear();
            foreach (var p in sorted)
                SortedPlayers.Add(p);
        });

        DataUpdated?.Invoke();
    }

    public void Dispose()
    {
        _updateTimer.Stop();
        EndSession();
    }
}
