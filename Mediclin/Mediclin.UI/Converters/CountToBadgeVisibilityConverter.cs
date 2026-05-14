using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mediclin.UI.Converters;

public class CountToBadgeVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var n = 0;
        if (value is int i)
        {
            n = i;
        }
        else if (value is not null && int.TryParse(value.ToString(), out var j))
        {
            n = j;
        }

        return n > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
