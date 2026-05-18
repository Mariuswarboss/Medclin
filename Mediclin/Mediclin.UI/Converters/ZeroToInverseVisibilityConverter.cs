using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mediclin.UI.Converters;

/// <summary>Shows Visible when count > 0, Collapsed when 0 (inverse of ZeroToVisibilityConverter).</summary>
public class ZeroToInverseVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is int i && i > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
