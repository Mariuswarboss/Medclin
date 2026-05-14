namespace Mediclin.Business.Validators;

public static class ProgramareValidator
{
    public static (bool valid, string error) ValidateDateTime(DateTime dataOra)
    {
        if (dataOra <= DateTime.Now)
        {
            return (false, "Programarea nu poate fi setata in trecut.");
        }

        if (dataOra.Hour < 7 || dataOra.Hour > 20)
        {
            return (false, "Programarea trebuie sa fie in intervalul 07:00 - 20:00.");
        }

        return (true, string.Empty);
    }

    public static (bool valid, string error) ValidateNoOverlap(
        DateTime dataOra,
        int durataMin,
        IEnumerable<(DateTime start, int durataMin)> programariExistente)
    {
        var end = dataOra.AddMinutes(durataMin);
        foreach (var programare in programariExistente)
        {
            var existingEnd = programare.start.AddMinutes(programare.durataMin);
            if (dataOra < existingEnd && end > programare.start)
            {
                return (false, "Medicul are deja o programare in acest interval.");
            }
        }

        return (true, string.Empty);
    }
}
