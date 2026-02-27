using AionPlugin.Models;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace AionPlugin.Services;

/// <summary>
/// 파티원 DPS를 추적하고 실시간 랭킹을 유지합니다.
/// EntityRegistry 와 연동해 EntityId → Player 이름을 해결합니다.
/// </summary>
public class DpsTracker : IDisposable
{
    private CombatSession? _currentSession;
    private readonly EntityRegistry _registry;
    private readonly DispatcherTimer _updateTimer;
    private bool _isInCombat;

    // 전투 비활성 타임아웃: 마지막 데미지 후 12초 (DataStorage.COMBAT_WINDOW_MS 동일)
    private const double CombatTimeoutSec = 12.0;
    private DateTime _lastDamageAt;

    /// <summary>UI 바인딩용 정렬된 플레이어 목록 (총 딜 내림차순)</summary>
    public ObservableCollection<Player> SortedPlayers { get; } = new();

    public bool IsInCombat => _isInCombat;
    public CombatSession? CurrentSession => _currentSession;

    public event Action? SessionStarted;
    public event Action? SessionEnded;
    public event Action? DataUpdated;

    public DpsTracker(EntityRegistry registry)
    {
        _registry = registry;
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _updateTimer.Tick += (_, _) => Tick();
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
        UpdateDisplayValues();
        SessionEnded?.Invoke();
    }

    public void ResetSession()
    {
        _currentSession = null;
        _isInCombat = false;
        _updateTimer.Stop();

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            SortedPlayers.Clear();
        });
    }

    // ──────────────────────────────────────────────────
    //  데미지 이벤트 처리
    // ──────────────────────────────────────────────────

    public void OnDamageEvent(DamageEvent evt)
    {
        if (evt.Damage <= 0) return;

        // 전투 중 아니면 자동 시작
        if (!_isInCombat)
            StartSession();

        _lastDamageAt = DateTime.UtcNow;
        _currentSession?.Events.Add(evt);

        // EntityRegistry 에 없으면 임시 등록
        _registry.GetOrCreate(evt.ActorId);
    }

    // ──────────────────────────────────────────────────
    //  주기 갱신
    // ──────────────────────────────────────────────────

    private void Tick()
    {
        // 마지막 데미지 후 CombatTimeoutSec 경과 시 세션 종료
        if (_isInCombat
            && _lastDamageAt != default
            && (DateTime.UtcNow - _lastDamageAt).TotalSeconds > CombatTimeoutSec)
        {
            EndSession();
            return;
        }

        UpdateDisplayValues();
    }

    private void UpdateDisplayValues()
    {
        if (_currentSession is null) return;

        double elapsed = _currentSession.ElapsedSeconds;
        var damageMap = _currentSession.DamageByAttacker()
            .ToDictionary(x => x.ActorId, x => x.Damage);

        long partyTotal = damageMap.Values.Sum();

        foreach (var (id, dmg) in damageMap)
        {
            var player = _registry.GetOrCreate(id);
            player.TotalDamage   = dmg;
            player.Dps           = dmg / elapsed;
            player.DamagePercent = partyTotal > 0 ? (double)dmg / partyTotal * 100.0 : 0;
        }

        var sorted = _registry.All
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
