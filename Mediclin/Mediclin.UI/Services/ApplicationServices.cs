using Mediclin.Business.Services;
using Mediclin.Data.Context;
using Mediclin.Data.Repositories;

namespace Mediclin.UI.Services;

/// <summary>Dependențe partajate pentru tot fluxul aplicației (după login).</summary>
public sealed class ApplicationServices
{
    public int? CurrentUserId { get; set; }

    public DatabaseContext Db { get; }
    public UtilizatorRepository Utilizatori { get; }
    public MedicRepository Medici { get; }
    public PacientRepository Pacienti { get; }
    public ProgramareRepository ProgramariRepo { get; }
    public ConsultatieRepository ConsultatiiRepo { get; }
    public RetetaRepository ReteteRepo { get; }
    public AnalizaRepository AnalizeRepo { get; }
    public MesajRepository MesajeRepo { get; }
    public SpecialitateRepository Specialitati { get; }
    public NotificareRepository NotificariRepo { get; }
    public JurnalRepository JurnalRepo { get; }
    public SetariRepository Setari { get; }

    public ProgramareService Programari { get; }
    public ConsultatieService Consultatii { get; }
    public RetetaService Retete { get; }
    public RaportService Rapoarte { get; }
    public NotificareService Notificari { get; }

    public ApplicationServices()
    {
        Db = new DatabaseContext();
        Utilizatori = new UtilizatorRepository(Db);
        Medici = new MedicRepository(Db);
        Pacienti = new PacientRepository(Db);
        ProgramariRepo = new ProgramareRepository(Db);
        ConsultatiiRepo = new ConsultatieRepository(Db);
        ReteteRepo = new RetetaRepository(Db);
        AnalizeRepo = new AnalizaRepository(Db);
        MesajeRepo = new MesajRepository(Db);
        Specialitati = new SpecialitateRepository(Db);
        NotificariRepo = new NotificareRepository(Db);
        JurnalRepo = new JurnalRepository(Db);
        Setari = new SetariRepository(Db);

        int? CurrentId() => CurrentUserId;

        Notificari = new NotificareService(NotificariRepo, JurnalRepo, CurrentId);
        Programari = new ProgramareService(ProgramariRepo, Medici, Pacienti, Notificari, JurnalRepo, CurrentId);
        Consultatii = new ConsultatieService(ConsultatiiRepo, ProgramariRepo, JurnalRepo, CurrentId);
        Retete = new RetetaService(ReteteRepo, Pacienti, Notificari, JurnalRepo, CurrentId);
        Rapoarte = new RaportService(Db);
    }
}
