namespace Mediclin.UI.Models;

public class CalendarDayItem
{
    public DateTime Date            { get; set; }
    public bool     IsCurrentMonth  { get; set; }
    public bool     IsToday         { get; set; }
    public bool     HasAppointments { get; set; }
    public bool     IsSelected      { get; set; }
    public string   DayNumber       => Date.Day.ToString();
    public bool     IsWeekend       =>
        Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
}
