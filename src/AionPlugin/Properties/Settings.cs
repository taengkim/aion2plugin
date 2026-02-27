using System.Configuration;

namespace AionPlugin.Properties;

/// <summary>
/// 사용자 설정 (자동 저장)
/// </summary>
[SettingsGroupName("AionPlugin")]
public sealed class Settings : ApplicationSettingsBase
{
    private static readonly Settings _default = (Settings)Synchronized(new Settings());
    public static Settings Default => _default;

    [UserScopedSetting]
    [DefaultSettingValue("")]
    public string ServerIp
    {
        get => (string)this[nameof(ServerIp)];
        set => this[nameof(ServerIp)] = value;
    }

    [UserScopedSetting]
    [DefaultSettingValue("")]
    public string ServerName
    {
        get => (string)this[nameof(ServerName)];
        set => this[nameof(ServerName)] = value;
    }

    [UserScopedSetting]
    [DefaultSettingValue("True")]
    public bool ShowAtuul
    {
        get => (bool)this[nameof(ShowAtuul)];
        set => this[nameof(ShowAtuul)] = value;
    }

    [UserScopedSetting]
    [DefaultSettingValue("0")]
    public double OverlayLeft
    {
        get => (double)this[nameof(OverlayLeft)];
        set => this[nameof(OverlayLeft)] = value;
    }

    [UserScopedSetting]
    [DefaultSettingValue("0")]
    public double OverlayTop
    {
        get => (double)this[nameof(OverlayTop)];
        set => this[nameof(OverlayTop)] = value;
    }

    [UserScopedSetting]
    [DefaultSettingValue("0.9")]
    public double OverlayOpacity
    {
        get => (double)this[nameof(OverlayOpacity)];
        set => this[nameof(OverlayOpacity)] = value;
    }
}
