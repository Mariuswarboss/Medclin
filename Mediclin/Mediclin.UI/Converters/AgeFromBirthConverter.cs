using System.Globalization;
using System.Windows.Data;

namespace Mediclin.UI.Converters;

/// <summary>Calculează vârsta din DateOnly (boxed) sau DateTime.</summary>
public class AgeFromBirthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        DateTime? birth = value switch
        {
            DateOnly d => d.ToDateTime(TimeOnly.MinValue),
            DateTime dt => dt.Date,
            _ => null
        };

        if (birth is null)
        {
            return string.Empty;
        }

        var today = DateTime.Today;
        var age = today.Year - birth.Value.Year;
        if (birth.Value.Date > today.AddYears(-age))
        {
            age--;
        }

        return $"{age} ani";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
