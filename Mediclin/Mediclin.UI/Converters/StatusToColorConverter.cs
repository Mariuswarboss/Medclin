using System.Globalization;
using System.Windows.Media;
using System.Windows.Data;

namespace Mediclin.UI.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value?.ToString()?.ToLowerInvariant();
        return status switch
        {
            "finalizata" => Brushes.ForestGreen,
            "anulata" => Brushes.Firebrick,
            "in_asteptare" => Brushes.DarkOrange,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
