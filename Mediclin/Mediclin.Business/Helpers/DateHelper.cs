namespace Mediclin.Business.Helpers;

public static class DateHelper
{
    private static readonly string[] RomanianWeekDays =
    {
        "Luni", "Marti", "Miercuri", "Joi", "Vineri", "Sambata", "Duminica"
    };

    public static string FormatDate(DateTime date) => date.ToString("dd.MM.yyyy");

    public static string FormatDateTime(DateTime date) => date.ToString("dd.MM.yyyy HH:mm");

    public static List<DateTime> GetWeekDays(DateTime referenceDate)
    {
        var diff = ((int)referenceDate.DayOfWeek + 6) % 7;
        var monday = referenceDate.Date.AddDays(-diff);
        return Enumerable.Range(0, 7).Select(day => monday.AddDays(day)).ToList();
    }

    public static string GetRomanianWeekDay(DateTime date)
    {
        var index = ((int)date.DayOfWeek + 6) % 7;
        return RomanianWeekDays[index];
    }

    public static bool IsInRange(DateTime value, DateTime start, DateTime end)
    {
        return value >= start && value <= end;
    }
}
