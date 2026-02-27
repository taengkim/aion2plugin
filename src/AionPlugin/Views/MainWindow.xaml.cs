using AionPlugin.Network;
using AionPlugin.Services;
using System.Windows;
using System.Windows.Interop;

namespace AionPlugin.Views;

public partial class MainWindow : Window
{
    private readonly PluginService _plugin;
    private OverlayWindow? _overlay;

    // 설정 바인딩용 프로퍼티
    public string ServerIp   { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _plugin = new PluginService();
        _plugin.StatusChanged += msg => Dispatcher.Invoke(() =>
        {
            TxtMainStatus.Text = msg;
            AppendLog(msg);
        });
        _plugin.ErrorOccurred += ex => Dispatcher.Invoke(() =>
        {
            TxtError.Text = ex.Message;
            TxtError.Visibility = Visibility.Visible;
            AppendLog($"[오류] {ex.Message}");
        });

        LoadDevices();
        LoadSettings();
    }

    // ──────────────────────────────────────────────────
    //  시작/중지
    // ──────────────────────────────────────────────────

    private void OnStartStop(object sender, RoutedEventArgs e)
    {
        if (_plugin.IsRunning)
        {
            _plugin.Stop();
            BtnStartStop.Content = "시작";
            BtnStartStop.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(26, 58, 106));
        }
        else
        {
            int deviceIdx = CmbDevice.SelectedIndex;
            string ip     = TxtServerIp.Text.Trim();
            string server = TxtServerName.Text.Trim();

            TxtError.Visibility = Visibility.Collapsed;

            _plugin.Start(deviceIdx, string.IsNullOrEmpty(ip) ? null : ip, server);

            BtnStartStop.Content = "중지";
            BtnStartStop.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(106, 26, 26));

            // 자동으로 오버레이 표시
            ShowOverlay();
        }
    }

    // ──────────────────────────────────────────────────
    //  오버레이
    // ──────────────────────────────────────────────────

    private void OnShowOverlay(object sender, RoutedEventArgs e) => ShowOverlay();

    private void ShowOverlay()
    {
        if (_overlay is null || !_overlay.IsLoaded)
        {
            _overlay = new OverlayWindow(_plugin.DpsTracker);
        }
        _overlay.Show();
        _overlay.Activate();
    }

    // ──────────────────────────────────────────────────
    //  오버레이 설정
    // ──────────────────────────────────────────────────

    private void OnAlwaysTopChanged(object sender, RoutedEventArgs e)
    {
        if (_overlay is not null)
            _overlay.Topmost = ChkAlwaysTop.IsChecked == true;
    }

    private void OnShowAtuulChanged(object sender, RoutedEventArgs e)
    {
        // 아툴 점수 컬럼 표시/숨김 처리 (오버레이에서 Visibility로 관리)
        Properties.Settings.Default.ShowAtuul = ChkShowAtuul.IsChecked == true;
        Properties.Settings.Default.Save();
    }

    private void OnClickThroughChanged(object sender, RoutedEventArgs e)
    {
        if (_overlay is null) return;

        bool clickThrough = ChkClickThrough.IsChecked == true;
        var hwnd = new WindowInteropHelper(_overlay).Handle;
        SetClickThrough(hwnd, clickThrough);
    }

    private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_overlay is not null)
            _overlay.Opacity = e.NewValue;
    }

    // ──────────────────────────────────────────────────
    //  창 제어
    // ──────────────────────────────────────────────────

    private void OnHide(object sender, RoutedEventArgs e) => Hide();

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // X 버튼 눌러도 숨기기만 (트레이 아이콘으로 계속 실행)
        e.Cancel = true;
        Hide();
    }

    // ──────────────────────────────────────────────────
    //  로드
    // ──────────────────────────────────────────────────

    private void LoadDevices()
    {
        try
        {
            var devices = PacketCapture.GetAvailableDevices().ToList();
            CmbDevice.ItemsSource = devices;
            if (devices.Count > 0)
                CmbDevice.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            AppendLog($"[경고] 어댑터 로드 실패: {ex.Message}");
        }
    }

    private void LoadSettings()
    {
        var s = Properties.Settings.Default;
        TxtServerIp.Text   = s.ServerIp;
        TxtServerName.Text = s.ServerName;
        ChkShowAtuul.IsChecked = s.ShowAtuul;
    }

    private void SaveSettings()
    {
        var s = Properties.Settings.Default;
        s.ServerIp   = TxtServerIp.Text.Trim();
        s.ServerName = TxtServerName.Text.Trim();
        s.Save();
    }

    // ──────────────────────────────────────────────────
    //  로그
    // ──────────────────────────────────────────────────

    private void AppendLog(string msg)
    {
        TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
        TxtLog.ScrollToEnd();
    }

    // ──────────────────────────────────────────────────
    //  Win32: 클릭 투과
    // ──────────────────────────────────────────────────

    private const int GWL_EXSTYLE    = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED    = 0x00080000;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    private static void SetClickThrough(IntPtr hwnd, bool enable)
    {
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        if (enable)
            style |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
        else
            style &= ~WS_EX_TRANSPARENT;
        SetWindowLong(hwnd, GWL_EXSTYLE, style);
    }
}
