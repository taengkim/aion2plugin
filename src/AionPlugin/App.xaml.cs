using AionPlugin.Views;
using System.Windows;

namespace AionPlugin;

public partial class App : Application
{
    private System.Windows.Forms.NotifyIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 시스템 트레이 아이콘 설정
        SetupTrayIcon();

        // 메인 창 (설정창)
        var main = new MainWindow();
        MainWindow = main;
        main.Show();
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Text    = "AionPlugin",
            Visible = true
        };

        // 아이콘 리소스가 없으면 기본 아이콘 사용
        try
        {
            _trayIcon.Icon = new System.Drawing.Icon(
                GetResourceStream(new Uri("Resources/icon.ico", UriKind.Relative)).Stream);
        }
        catch
        {
            _trayIcon.Icon = System.Drawing.SystemIcons.Application;
        }

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("설정 열기", null, (_, _) =>
        {
            MainWindow?.Show();
            MainWindow?.Activate();
        });
        menu.Items.Add("오버레이 표시", null, (_, _) =>
        {
            if (MainWindow is MainWindow mw)
                mw.GetType().GetMethod("ShowOverlay",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(mw, null);
        });
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("종료", null, (_, _) =>
        {
            _trayIcon.Visible = false;
            Shutdown();
        });

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) =>
        {
            MainWindow?.Show();
            MainWindow?.Activate();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
