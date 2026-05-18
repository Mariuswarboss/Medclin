namespace Mediclin.UI.Models;

public class ProgramareDisplay
{
    public int      Id           { get; set; }
    public DateTime DataOra      { get; set; }
    public int      DurataMin    { get; set; }
    public string   Tip          { get; set; } = "";
    public string   Status       { get; set; } = "";
    public string   MotivVizita  { get; set; } = "";
    public string   PacientNume  { get; set; } = "";
    public string   MedicNume    { get; set; } = "";
    public string   MedicTitlu   { get; set; } = "";
    public string   SpecialitateNume { get; set; } = "";

    // Computed helpers
    public string OraText =>
        DataOra.ToString("HH:mm");
    public string DataText =>
        DataOra.ToString("dd MMM yyyy");
    public string ZiNumar =>
        DataOra.ToString("dd");
    public string LunaText =>
        DataOra.ToString("MMM").ToUpper();
    public string MedicComplet =>
        string.IsNullOrWhiteSpace(MedicTitlu)
            ? MedicNume
            : $"{MedicTitlu} {MedicNume}";
    public bool IsFuture =>
        DataOra > DateTime.Now;
    public bool CanCancel =>
        IsFuture && Status is not
            ("Anulata" or "Finalizata" or "Neprezentata");

    // Legacy compat helpers used by MyAppointmentsView
    private static readonly System.Globalization.CultureInfo Ro =
        new("ro-RO");
    public string DateLabel => DataOra.ToString("dd MMM", Ro);
    public string TimeLabel => DataOra.ToString("HH:mm");
    public string DoctorName =>
        string.IsNullOrWhiteSpace(MedicTitlu) ? MedicNume : $"{MedicTitlu} {MedicNume}";
    public string Specialty => SpecialitateNume;
    public string Reason    => MotivVizita;
    public string Initiale  { get; set; } = "";
}
