namespace AionPlugin.Models;

/// <summary>
/// aion2tools.com 에서 제공하는 캐릭터 점수 정보
/// </summary>
public class AtuulScore
{
    /// <summary>캐릭터명</summary>
    public string CharacterName { get; set; } = string.Empty;

    /// <summary>서버명</summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>종합 점수</summary>
    public double Score { get; set; }

    /// <summary>점수 등급 (S, A, B, C, D)</summary>
    public string Grade { get; set; } = string.Empty;

    /// <summary>장비 점수</summary>
    public double GearScore { get; set; }

    /// <summary>전투력</summary>
    public long CombatPower { get; set; }

    /// <summary>직업</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>레벨</summary>
    public int Level { get; set; }

    /// <summary>마지막 갱신 시각</summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>aion2tools.com 프로필 링크</summary>
    public string ProfileUrl { get; set; } = string.Empty;

    // 표시용
    public string ScoreDisplay => Score > 0 ? $"{Score:F0}" : "N/A";
    public string GradeDisplay => !string.IsNullOrEmpty(Grade) ? Grade : "?";

    public System.Windows.Media.SolidColorBrush GradeColor => Grade switch
    {
        "S" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 215, 0)),   // Gold
        "A" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 100, 100)), // Red-ish
        "B" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 180, 255)), // Blue
        "C" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(150, 255, 150)), // Green
        _   => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200))  // Grey
    };
}
