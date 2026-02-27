using AionPlugin.Models;
using AionPlugin.Network;

namespace AionPlugin.Services;

/// <summary>
/// 플러그인의 핵심 서비스 오케스트레이터
/// PacketCapture → PacketParser → DpsTracker + AionToolsService 를 연결합니다.
/// </summary>
public class PluginService : IDisposable
{
    private readonly PacketCapture _capture = new();
    private readonly PacketParser _parser = new();
    public readonly DpsTracker DpsTracker = new();
    public readonly AionToolsService AionTools = new();

    private CancellationTokenSource _cts = new();
    private string _serverName = string.Empty;

    public bool IsRunning => _capture.IsCapturing;

    public event Action<string>? StatusChanged;
    public event Action<Exception>? ErrorOccurred;

    public PluginService()
    {
        // 파서 이벤트 연결
        _parser.DamageEventParsed += OnDamageEvent;
        _parser.PlayerInfoParsed  += OnPlayerInfo;
        _parser.PartyInfoParsed   += OnPartyInfo;
        _parser.CombatStarted     += () => DpsTracker.StartSession();
        _parser.CombatEnded       += () => DpsTracker.EndSession();

        // 캡처 이벤트 연결
        _capture.PacketReceived += raw => _parser.Feed(raw);
        _capture.CaptureError   += ex => ErrorOccurred?.Invoke(ex);
        _capture.CaptureStarted += () => StatusChanged?.Invoke("캡처 시작됨");
        _capture.CaptureStopped += () => StatusChanged?.Invoke("캡처 중지됨");
    }

    /// <summary>
    /// 플러그인 시작
    /// </summary>
    public void Start(int deviceIndex = -1, string? serverIp = null, string serverName = "")
    {
        _serverName = serverName;
        _cts = new CancellationTokenSource();

        try
        {
            _capture.StartCapture(deviceIndex, serverIp);
            StatusChanged?.Invoke("아이온2 패킷 감청 중...");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
            StatusChanged?.Invoke($"오류: {ex.Message}");
        }
    }

    public void Stop()
    {
        _capture.StopCapture();
        DpsTracker.EndSession();
        _cts.Cancel();
    }

    public void ResetDps() => DpsTracker.ResetSession();

    // ──────────────────────────────────────────────────
    //  파서 이벤트 핸들러
    // ──────────────────────────────────────────────────

    private void OnDamageEvent(DamageEvent evt)
    {
        DpsTracker.OnDamageEvent(evt);
    }

    private void OnPlayerInfo(Player player)
    {
        DpsTracker.RegisterPlayer(player);
        LoadAtuulScoreAsync(player);
    }

    private void OnPartyInfo(List<Player> players)
    {
        DpsTracker.RegisterParty(players);
        foreach (var p in players)
            LoadAtuulScoreAsync(p);
    }

    private void LoadAtuulScoreAsync(Player player)
    {
        if (string.IsNullOrEmpty(player.Name) || player.Name.StartsWith("Unknown"))
            return;

        player.IsLoadingAtuul = true;

        Task.Run(async () =>
        {
            try
            {
                var score = await AionTools.GetScoreAsync(player.Name, _serverName, _cts.Token);
                player.AtuulScore = score;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
            finally
            {
                player.IsLoadingAtuul = false;
            }
        });
    }

    public void Dispose()
    {
        Stop();
        _capture.Dispose();
        DpsTracker.Dispose();
        AionTools.Dispose();
        _cts.Dispose();
    }
}
