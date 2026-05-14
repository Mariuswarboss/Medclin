using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Mediclin.UI.Converters;

public class AnalyzeStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value?.ToString()?.Trim().ToLowerInvariant();
        return s switch
        {
            "normal" => new SolidColorBrush(Color.FromRgb(39, 174, 96)),
            "ridicat" => new SolidColorBrush(Color.FromRgb(239, 159, 39)),
            "scazut" => new SolidColorBrush(Color.FromRgb(55, 138, 221)),
            "critic" => new SolidColorBrush(Color.FromRgb(192, 57, 43)),
            _ => new SolidColorBrush(Color.FromRgb(136, 135, 128))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
