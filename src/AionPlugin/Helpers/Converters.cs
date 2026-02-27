using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AionPlugin.Helpers;

/// <summary>
/// DamagePercent → 픽셀 너비 (딜 바 렌더링)
/// </summary>
[ValueConversion(typeof(double), typeof(double))]
public class PercentToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double percent) return 0.0;
        double maxWidth = parameter is string s && double.TryParse(s, out double w) ? w : 300.0;
        return Math.Max(0, Math.Min(percent / 100.0 * maxWidth, maxWidth));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// bool → Visibility (true = Visible, false = Collapsed)
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// null 여부 → Visibility (null이 아닐 때 Visible)
/// </summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value is null;
        bool visible = Invert ? isNull : !isNull;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// ItemsControl 내 순위 번호 표시용 (간단 구현)
/// </summary>
[ValueConversion(typeof(object), typeof(string))]
public class ListIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // 실제로는 AlternationIndex 를 쓰는 것이 더 정확
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
