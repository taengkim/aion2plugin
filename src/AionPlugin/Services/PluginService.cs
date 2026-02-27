using AionPlugin.Models;
using AionPlugin.Network;

namespace AionPlugin.Services;

/// <summary>
/// 플러그인 핵심 오케스트레이터
///
/// 파이프라인:
///   PacketCapture (TCP 청크)
///     → PacketParser (VarInt 파싱 → DamageEvent / Nickname)
///     → EntityRegistry (EntityId → Player 이름 관리)
///     → DpsTracker (세션 집계)
///     → AionToolsService (아툴 점수 비동기 조회)
/// </summary>
public class PluginService : IDisposable
{
    private readonly PacketCapture      _capture  = new();
    private readonly PacketParser       _parser   = new();
    public  readonly EntityRegistry     Registry  = new();
    public  readonly AionToolsService   AionTools = new();
    public  readonly DpsTracker         DpsTracker;

    private CancellationTokenSource _cts = new();
    private string _serverName = string.Empty;

    public bool IsRunning => _capture.IsCapturing;

    public event Action<string>?    StatusChanged;
    public event Action<Exception>? ErrorOccurred;

    public PluginService()
    {
        DpsTracker = new DpsTracker(Registry);

        // 파서 → 서비스 연결
        _parser.DamageEventParsed += OnDamageEvent;
        _parser.NicknameParsed    += OnNickname;

        // 캡처 → 파서 연결 (서버→클 TCP 청크만 전달)
        _capture.ServerChunkReceived += chunk => _parser.Feed(chunk);
        _capture.CaptureError        += ex   => ErrorOccurred?.Invoke(ex);
        _capture.CaptureStarted      += ()   => StatusChanged?.Invoke("패킷 캡처 시작됨");
        _capture.CaptureStopped      += ()   => StatusChanged?.Invoke("패킷 캡처 중지됨");
    }

    // ──────────────────────────────────────────────────
    //  시작 / 중지
    // ──────────────────────────────────────────────────

    /// <summary>
    /// 플러그인 시작
    /// </summary>
    /// <param name="deviceIndex">어댑터 인덱스 (-1 = 자동)</param>
    /// <param name="serverNetCidr">서버 CIDR (null = 기본값 206.127.156.0/24)</param>
    /// <param name="serverName">아툴 점수 조회용 서버명</param>
    public void Start(int deviceIndex = -1, string? serverNetCidr = null, string serverName = "")
    {
        _serverName = serverName;
        _cts = new CancellationTokenSource();

        try
        {
            _capture.StartCapture(deviceIndex, serverNetCidr);
            StatusChanged?.Invoke(
                $"아이온2 감청 중... (포트 {PacketCapture.DefaultPort}, {serverNetCidr ?? PacketCapture.DefaultNetCidr})");
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

    public void Reset()
    {
        DpsTracker.ResetSession();
        Registry.Clear();
    }

    // ──────────────────────────────────────────────────
    //  이벤트 핸들러
    // ──────────────────────────────────────────────────

    private void OnDamageEvent(DamageEvent evt)
    {
        DpsTracker.OnDamageEvent(evt);
    }

    private void OnNickname((int EntityId, string Name) info)
    {
        Registry.UpdateNickname(info.EntityId, info.Name);

        // 닉네임 확정 시 아툴 점수 조회
        var player = Registry.Find(info.EntityId);
        if (player is not null)
            LoadAtuulScoreAsync(player);
    }

    private void LoadAtuulScoreAsync(Player player)
    {
        if (string.IsNullOrEmpty(player.Name) || player.Name.StartsWith("User_"))
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
            catch (Exception ex) { ErrorOccurred?.Invoke(ex); }
            finally { player.IsLoadingAtuul = false; }
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
