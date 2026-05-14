namespace Mediclin.Data.Models;

public class Conversatie
{
    public int Id { get; set; }
    public int PacientId { get; set; }
    public int MedicId { get; set; }
    public DateTime CreatLa { get; set; }
}
