using AionPlugin.Services;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AionPlugin.Views;

public partial class OverlayWindow : Window
{
    private readonly DpsTracker _tracker;
    private readonly DispatcherTimer _timerClock;
    private bool _isDragLocked;

    public OverlayWindow(DpsTracker tracker)
    {
        InitializeComponent();
        _tracker = tracker;

        // DataContext를 DpsTracker에 바인딩
        DataContext = tracker;

        // 전투 타이머 클럭
        _timerClock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timerClock.Tick += UpdateTimer;
        _timerClock.Start();

        // DPS 데이터 갱신 시 하단 합산 업데이트
        tracker.DataUpdated += () => Dispatcher.Invoke(UpdateFooter);
        tracker.SessionStarted += () => Dispatcher.Invoke(() =>
        {
            TxtStatus.Text = "● 전투 중";
            TxtStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 80, 80));
        });
        tracker.SessionEnded += () => Dispatcher.Invoke(() =>
        {
            TxtStatus.Text = "■ 전투 종료";
            TxtStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(138, 138, 160));
        });

        // 창 위치 복원
        RestorePosition();
    }

    // ──────────────────────────────────────────────────
    //  타이머 / 푸터
    // ──────────────────────────────────────────────────

    private void UpdateTimer(object? sender, EventArgs e)
    {
        if (_tracker.CurrentSession is { } session)
        {
            var d = session.Duration;
            TxtTimer.Text = $"{(int)d.TotalMinutes:D2}:{d.Seconds:D2}";
        }
        else
        {
            TxtTimer.Text = "00:00";
        }
    }

    private void UpdateFooter()
    {
        if (_tracker.CurrentSession is { } session)
        {
            TxtPartyTotal.Text = $"파티 총 딜: {FormatNumber(session.TotalDamage)}";
        }
    }

    // ──────────────────────────────────────────────────
    //  버튼 이벤트
    // ──────────────────────────────────────────────────

    private void OnReset(object sender, RoutedEventArgs e) => _tracker.ResetSession();

    private void OnSettings(object sender, RoutedEventArgs e)
    {
        // MainWindow(설정창)를 앞으로 가져옴
        if (Application.Current.MainWindow is { } main && main != this)
        {
            main.Show();
            main.Activate();
        }
    }

    private void OnClose(object sender, RoutedEventArgs e) => Hide();

    private void OnToggleLock(object sender, RoutedEventArgs e)
    {
        _isDragLocked = !_isDragLocked;
        BtnLock.Content = _isDragLocked ? "🔒" : "🔓";
        BtnLock.ToolTip = _isDragLocked ? "드래그 잠금 해제" : "드래그 잠금";
    }

    // ──────────────────────────────────────────────────
    //  드래그 이동
    // ──────────────────────────────────────────────────

    private void OnDragMove(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragLocked && e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    // ──────────────────────────────────────────────────
    //  창 위치 저장/복원
    // ──────────────────────────────────────────────────

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        SavePosition();
    }

    private void SavePosition()
    {
        Properties.Settings.Default.OverlayLeft = Left;
        Properties.Settings.Default.OverlayTop = Top;
        Properties.Settings.Default.Save();
    }

    private void RestorePosition()
    {
        double left = Properties.Settings.Default.OverlayLeft;
        double top  = Properties.Settings.Default.OverlayTop;

        if (left > 0 || top > 0)
        {
            Left = left;
            Top  = top;
        }
    }

    // ──────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────

    private static string FormatNumber(long n) => n switch
    {
        >= 1_000_000_000 => $"{n / 1_000_000_000.0:F2}B",
        >= 1_000_000     => $"{n / 1_000_000.0:F2}M",
        >= 1_000         => $"{n / 1_000.0:F1}K",
        _                => n.ToString("N0")
    };
}
