using SharpPcap;
using SharpPcap.LibPcap;
using PacketDotNet;
using System.Net;

namespace AionPlugin.Network;

/// <summary>
/// Npcap 을 통해 아이온2 게임 서버와 클라이언트 간 패킷을 캡처합니다.
/// 실행하려면 Npcap (https://npcap.com) 이 설치되어 있어야 합니다.
/// </summary>
public class PacketCapture : IDisposable
{
    // 아이온2 서버 포트 (실제 포트로 업데이트 필요)
    private const int Aion2GamePort = 2106;  // 예시 포트

    private ILiveDevice? _device;
    private CancellationTokenSource? _cts;

    public event Action<RawPacketData>? PacketReceived;
    public event Action<Exception>? CaptureError;
    public event Action? CaptureStarted;
    public event Action? CaptureStopped;

    public bool IsCapturing { get; private set; }

    /// <summary>
    /// 사용 가능한 네트워크 어댑터 목록 반환
    /// </summary>
    public static IEnumerable<DeviceInfo> GetAvailableDevices()
    {
        var devices = CaptureDeviceList.Instance;
        return devices.Select((d, i) => new DeviceInfo
        {
            Index = i,
            Name = d.Name,
            Description = d.Description ?? d.Name,
            IsLoopback = d.Name.Contains("loopback", StringComparison.OrdinalIgnoreCase)
        });
    }

    /// <summary>
    /// 패킷 캡처 시작
    /// </summary>
    /// <param name="deviceIndex">어댑터 인덱스 (-1 이면 자동 선택)</param>
    /// <param name="serverIp">게임 서버 IP (null 이면 포트 기반 필터링만)</param>
    public void StartCapture(int deviceIndex = -1, string? serverIp = null)
    {
        if (IsCapturing)
            return;

        var devices = CaptureDeviceList.Instance;
        if (devices.Count == 0)
            throw new InvalidOperationException("네트워크 어댑터를 찾을 수 없습니다. Npcap이 설치되어 있는지 확인하세요.");

        _device = deviceIndex >= 0 && deviceIndex < devices.Count
            ? devices[deviceIndex]
            : SelectBestDevice(devices);

        // BPF 필터: 게임 서버 포트 기반
        string filter = BuildBpfFilter(serverIp);

        _device.Open(DeviceModes.Promiscuous, 1000);
        _device.Filter = filter;
        _device.OnPacketArrival += OnPacketArrival;

        _cts = new CancellationTokenSource();
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
        _cts?.Cancel();
    }

    private void OnPacketArrival(object sender, PacketCapture e)
    {
        try
        {
            var rawPacket = e.GetPacket();
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            var tcpPacket = packet.Extract<TcpPacket>();

            if (tcpPacket?.PayloadData is { Length: > 0 } payload)
            {
                bool isFromServer = tcpPacket.SourcePort == Aion2GamePort;
                PacketReceived?.Invoke(new RawPacketData
                {
                    Data = payload,
                    IsFromServer = isFromServer,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            CaptureError?.Invoke(ex);
        }
    }

    private static ILiveDevice SelectBestDevice(CaptureDeviceList devices)
    {
        // 루프백이 아닌 첫 번째 어댑터 선택
        return devices.FirstOrDefault(d =>
            !d.Name.Contains("loopback", StringComparison.OrdinalIgnoreCase)) ?? devices[0];
    }

    private static string BuildBpfFilter(string? serverIp)
    {
        string portFilter = $"tcp port {Aion2GamePort}";
        if (!string.IsNullOrEmpty(serverIp))
            return $"host {serverIp} and {portFilter}";
        return portFilter;
    }

    public void Dispose()
    {
        StopCapture();
        _device?.Dispose();
        _cts?.Dispose();
    }
}

public record RawPacketData
{
    public byte[] Data { get; init; } = Array.Empty<byte>();
    public bool IsFromServer { get; init; }
    public DateTime Timestamp { get; init; }
}

public class DeviceInfo
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsLoopback { get; set; }
    public override string ToString() => Description;
}
