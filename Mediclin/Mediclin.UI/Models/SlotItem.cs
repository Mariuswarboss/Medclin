namespace Mediclin.UI.Models;

/// <summary>
/// Represents a single bookable time slot shown in the patient booking UI.
/// </summary>
public sealed class SlotItem
{
    public SlotItem(DateTime at)
    {
        At = at;
    }

    public DateTime At       { get; }
    public string   HourText => At.ToString("HH:mm");
}
