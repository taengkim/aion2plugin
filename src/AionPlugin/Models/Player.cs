using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AionPlugin.Models;

/// <summary>
/// 파티원 플레이어 정보
/// </summary>
public class Player : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _className = string.Empty;
    private long _totalDamage;
    private double _dps;
    private double _damagePercent;
    private AtuulScore? _atuulScore;
    private bool _isLoadingAtuul;
    private bool _isSelf;

    public uint ObjectId { get; set; }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string ClassName
    {
        get => _className;
        set { _className = value; OnPropertyChanged(); OnPropertyChanged(nameof(ClassIcon)); }
    }

    /// <summary>
    /// 전투 시작부터 누적 총 데미지
    /// </summary>
    public long TotalDamage
    {
        get => _totalDamage;
        set { _totalDamage = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalDamageDisplay)); }
    }

    /// <summary>
    /// 초당 데미지 (DPS)
    /// </summary>
    public double Dps
    {
        get => _dps;
        set { _dps = value; OnPropertyChanged(); OnPropertyChanged(nameof(DpsDisplay)); }
    }

    /// <summary>
    /// 파티 전체 대비 데미지 비율 (%)
    /// </summary>
    public double DamagePercent
    {
        get => _damagePercent;
        set { _damagePercent = value; OnPropertyChanged(); OnPropertyChanged(nameof(DamagePercentDisplay)); }
    }

    /// <summary>
    /// aion2tools.com 아툴 점수
    /// </summary>
    public AtuulScore? AtuulScore
    {
        get => _atuulScore;
        set { _atuulScore = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 아툴 점수 로딩 중 여부
    /// </summary>
    public bool IsLoadingAtuul
    {
        get => _isLoadingAtuul;
        set { _isLoadingAtuul = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 본인 여부
    /// </summary>
    public bool IsSelf
    {
        get => _isSelf;
        set { _isSelf = value; OnPropertyChanged(); }
    }

    // --- Display helpers ---

    public string TotalDamageDisplay => FormatNumber(TotalDamage);
    public string DpsDisplay => $"{Dps:N0}";
    public string DamagePercentDisplay => $"{DamagePercent:F1}%";

    public string ClassIcon => ClassName switch
    {
        "Warrior"     => "⚔",
        "Assassin"    => "🗡",
        "Ranger"      => "🏹",
        "Mage"        => "🔮",
        "Healer"      => "✚",
        "Summoner"    => "🌿",
        "Chanter"     => "🎵",
        _             => "●"
    };

    private static string FormatNumber(long n) => n switch
    {
        >= 1_000_000_000 => $"{n / 1_000_000_000.0:F2}B",
        >= 1_000_000     => $"{n / 1_000_000.0:F2}M",
        >= 1_000         => $"{n / 1_000.0:F1}K",
        _                => n.ToString("N0")
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
