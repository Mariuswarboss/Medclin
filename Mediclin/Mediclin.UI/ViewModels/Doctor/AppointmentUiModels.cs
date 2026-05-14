namespace Mediclin.UI.ViewModels.Doctor;

public sealed class SlotItem
{
    public SlotItem(DateTime at)
    {
        At = at;
    }

    public DateTime At { get; }
    public string HourText => At.ToString("HH:mm");
}

public sealed class CalendarDayItem
{
    public DateTime Date { get; init; }
    public int DayNumber => Date.Day;
    public bool IsCurrentMonth { get; init; }
    public bool IsToday { get; init; }
    public bool HasAppointments { get; init; }
    public bool IsSelected { get; init; }
}
