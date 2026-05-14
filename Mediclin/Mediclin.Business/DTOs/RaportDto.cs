namespace Mediclin.Business.DTOs;

public class RaportDto
{
    public int TotalConsultatii { get; set; }
    public int PacientiNoi { get; set; }
    public decimal RataNeprezentare { get; set; }
    public decimal VenitEstimat { get; set; }
    public List<ConsultatiiPerZiDto> ConsultatiiPerZi { get; set; } = new();

    /// <summary>Agregate programări (medic / zi sau lună).</summary>
    public int TotalProgramari { get; set; }
    public int ProgramariFinalizate { get; set; }
    public int ProgramariNeprezentate { get; set; }

    /// <summary>Statistici panou admin.</summary>
    public int UtilizatoriActivi { get; set; }
    public int MediciVerificati { get; set; }
    public int ProgramariAstazi { get; set; }
}

public class ConsultatiiPerZiDto
{
    public DateTime Data { get; set; }
    public int Total { get; set; }
}
