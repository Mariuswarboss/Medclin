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
            "true" => new SolidColorBrush(Color.FromRgb(225, 245, 238)),
            "false" => Brushes.White,
            "finalizata" => new SolidColorBrush(Color.FromRgb(92, 92, 90)),
            "anulata" => new SolidColorBrush(Color.FromRgb(192, 57, 43)),
            "neprezentata" => new SolidColorBrush(Color.FromRgb(192, 57, 43)),
            "in_cabinet" => new SolidColorBrush(Color.FromRgb(15, 110, 86)),
            "in_asteptare" => new SolidColorBrush(Color.FromRgb(239, 159, 39)),
            "programata" => new SolidColorBrush(Color.FromRgb(55, 138, 221)),
            "confirmata" => new SolidColorBrush(Color.FromRgb(29, 158, 117)),
            "activa" => new SolidColorBrush(Color.FromRgb(15, 110, 86)),
            "normal" => new SolidColorBrush(Color.FromRgb(15, 110, 86)),
            "ridicat" => new SolidColorBrush(Color.FromRgb(239, 159, 39)),
            "scazut" => new SolidColorBrush(Color.FromRgb(55, 138, 221)),
            "critic" => new SolidColorBrush(Color.FromRgb(192, 57, 43)),
            _ => new SolidColorBrush(Color.FromRgb(92, 92, 90))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
