using SharpPcap;
using SharpPcap.LibPcap;
using PacketDotNet;

namespace AionPlugin.Network;

/// <summary>
/// Npcap 을 통해 아이온2 게임 서버와 클라이언트 간 패킷을 캡처합니다.
/// 실행하려면 Npcap (https://npcap.com) 이 설치되어 있어야 합니다.
///
/// 서버 정보 출처: AION2Meter4J PcapCapturerConfig.kt
///   - 서버 IP:   206.127.156.0/24
///   - 서버 포트: 13328
/// </summary>
public class PacketCapture : IDisposable
{
    // AION2Meter4J 기준 실제 서버 정보
    public const int    DefaultPort    = 13328;
    public const string DefaultNetCidr = "206.127.156.0/24";

    private ILiveDevice? _device;

    public event Action<byte[]>? ServerChunkReceived; // 서버→클 TCP 데이터만 전달
    public event Action<Exception>? CaptureError;
    public event Action? CaptureStarted;
    public event Action? CaptureStopped;

    public bool IsCapturing { get; private set; }

    /// <summary>사용 가능한 네트워크 어댑터 목록 반환</summary>
    public static IEnumerable<DeviceInfo> GetAvailableDevices()
    {
        return CaptureDeviceList.Instance.Select((d, i) => new DeviceInfo
        {
            Index       = i,
            Name        = d.Name,
            Description = d.Description ?? d.Name,
            IsLoopback  = d.Name.Contains("loopback", StringComparison.OrdinalIgnoreCase)
        });
    }

    /// <summary>
    /// 패킷 캡처 시작
    /// </summary>
    /// <param name="deviceIndex">어댑터 인덱스 (-1 이면 자동 선택)</param>
    /// <param name="serverNetCidr">서버 CIDR (null 이면 기본값 사용)</param>
    /// <param name="port">포트 (0 이면 기본값 사용)</param>
    public void StartCapture(int deviceIndex = -1, string? serverNetCidr = null, int port = 0)
    {
        if (IsCapturing) return;

        var devices = CaptureDeviceList.Instance;
        if (devices.Count == 0)
            throw new InvalidOperationException(
                "네트워크 어댑터를 찾을 수 없습니다. Npcap 이 설치되어 있는지 확인하세요.");

        _device = deviceIndex >= 0 && deviceIndex < devices.Count
            ? devices[deviceIndex]
            : SelectBestDevice(devices);

        int actualPort = port > 0 ? port : DefaultPort;
        string actualNet = !string.IsNullOrEmpty(serverNetCidr) ? serverNetCidr : DefaultNetCidr;

        // BPF 필터: AION2Meter4J 와 동일 패턴
        string filter = $"tcp and net {actualNet} and (port {actualPort})";

        _device.Open(DeviceModes.Promiscuous, timeoutMilliseconds: 10);
        _device.Filter = filter;
        _device.OnPacketArrival += OnPacketArrival;

        IsCapturing = true;
        CaptureStarted?.Invoke();
        _device.StartCapture();
    }

    public void StopCapture()
    {
        if (!IsCapturing) return;

        _device?.StopCapture();
        _device?.Close();
        IsCapturing = false;
        CaptureStopped?.Invoke();
    }

    private void OnPacketArrival(object sender, PacketCapture e)
    {
        try
        {
            var rawPacket = e.GetPacket();
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            var tcp = packet.Extract<TcpPacket>();

            if (tcp?.PayloadData is { Length: > 0 } payload)
            {
                // BPF 로 이미 서버 IP/포트 필터링됨.
                // 서버→클 방향: 소스 포트가 게임 포트
                bool isFromServer = tcp.SourcePort == DefaultPort;
                if (isFromServer)
                    ServerChunkReceived?.Invoke(payload);
            }
        }
        catch (Exception ex)
        {
            CaptureError?.Invoke(ex);
        }
    }

    // ──────────────────────────────────────────────────
    //  장치 자동 선택 (PcapCapturer.kt 전략과 동일)
    // ──────────────────────────────────────────────────
    private static ILiveDevice SelectBestDevice(CaptureDeviceList devices)
    {
        string[] ignoreKeywords = ["bluetooth", "virtual", "vmware", "vpn", "loopback",
                                   "hyper-v", "npcap loopback"];

        bool IsSuspicious(ILiveDevice d)
        {
            string desc = (d.Description ?? "").ToLower();
            string name = d.Name.ToLower();
            return ignoreKeywords.Any(k => desc.Contains(k) || name.Contains(k));
        }

        // 유효한 IPv4 + 비-의심 어댑터 우선
        if (devices.FirstOrDefault(d => !IsSuspicious(d) && d is LibPcapLiveDevice ld
                && ld.Interface?.Addresses?.Any(a =>
                    a.Addr?.ipAddress?.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    && !System.Net.IPAddress.IsLoopback(a.Addr.ipAddress)) == true)
            is { } best)
            return best;

        // 없으면 첫 번째 비-루프백
        return devices.FirstOrDefault(d => !IsSuspicious(d)) ?? devices[0];
    }

    public void Dispose()
    {
        StopCapture();
        _device?.Dispose();
    }
}

public class DeviceInfo
{
    public int    Index       { get; set; }
    public string Name        { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool   IsLoopback  { get; set; }
    public override string ToString() => Description;
}
