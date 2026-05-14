namespace Mediclin.UI.ViewModels.Doctor;

public sealed class RaportBarItem
{
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
    public double BarHeight { get; init; }
    public double BarWidth { get; init; } = 20;
}
