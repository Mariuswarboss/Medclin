using System.Collections.ObjectModel;
using System.Windows.Input;
using Mediclin.Business.Services;
using Mediclin.Data.Models;
using Mediclin.UI.ViewModels.Auth;
using Mediclin.UI.Views.Auth;
using Mediclin.UI.Views.Admin;
using Mediclin.UI.Views.Doctor;
using Mediclin.UI.Views.Patient;

namespace Mediclin.UI.ViewModels;

public class RoleMainViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private object? _currentView;
    private string _activeSection = string.Empty;
    private string _statusMessage = "Sistem pregatit.";
    private string _chatText = string.Empty;

    public Utilizator Utilizator { get; }
    public string NumeComplet => Utilizator.NumeComplet;
    public string MesajBunVenit => $"Bun venit, {Utilizator.NumeComplet}! Rolul tau: {Utilizator.Rol}";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string ChatText
    {
        get => _chatText;
        set => SetProperty(ref _chatText, value);
    }

    public ObservableCollection<MetricItem> Metrics { get; } = new();
    public ObservableCollection<AppointmentItem> Appointments { get; } = new();
    public ObservableCollection<PrescriptionItem> Prescriptions { get; } = new();
    public ObservableCollection<LabResultItem> LabResults { get; } = new();
    public ObservableCollection<MessageItem> Messages { get; } = new();
    public ObservableCollection<UserAdminItem> Users { get; } = new();
    public ObservableCollection<DoctorApprovalItem> DoctorApprovals { get; } = new();
    public ObservableCollection<AuditLogItem> AuditLogs { get; } = new();
    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }
    public string ActiveSection
    {
        get => _activeSection;
        set => SetProperty(ref _activeSection, value);
    }

    public ICommand LogoutCommand { get; }
    public ICommand NavigateCommand { get; }
    public ICommand AddAppointmentCommand { get; }
    public ICommand CancelAppointmentCommand { get; }
    public ICommand UpdateAppointmentStatusCommand { get; }
    public ICommand SaveMedicalRecordCommand { get; }
    public ICommand IssuePrescriptionCommand { get; }
    public ICommand SendMessageCommand { get; }
    public ICommand AddUserCommand { get; }
    public ICommand ToggleUserCommand { get; }
    public ICommand ApproveDoctorCommand { get; }
    public ICommand ExportCommand { get; }
    public Action? CloseAction { get; set; }

    public RoleMainViewModel(Utilizator utilizator, IAuthService authService)
    {
        Utilizator = utilizator;
        _authService = authService;
        SeedWorkspace();
        LogoutCommand = new RelayCommand(_ => Logout());
        NavigateCommand = new RelayCommand(section => Navigate(section?.ToString() ?? string.Empty));
        AddAppointmentCommand = new RelayCommand(_ => AddAppointment());
        CancelAppointmentCommand = new RelayCommand(item => CancelAppointment(item as AppointmentItem));
        UpdateAppointmentStatusCommand = new RelayCommand(value => UpdateAppointmentStatus(value?.ToString() ?? "Finalizata"));
        SaveMedicalRecordCommand = new RelayCommand(_ => SetStatus("Fisa medicala a fost salvata si marcata ca verificata."));
        IssuePrescriptionCommand = new RelayCommand(_ => IssuePrescription());
        SendMessageCommand = new RelayCommand(_ => SendMessage(), _ => !string.IsNullOrWhiteSpace(ChatText));
        AddUserCommand = new RelayCommand(_ => AddUser());
        ToggleUserCommand = new RelayCommand(item => ToggleUser(item as UserAdminItem));
        ApproveDoctorCommand = new RelayCommand(item => ApproveDoctor(item as DoctorApprovalItem));
        ExportCommand = new RelayCommand(kind => SetStatus($"Exportul {kind ?? "raportului"} a fost generat in Documente/MediClin."));
        Navigate(DefaultSectionForRole(utilizator.Rol));
    }

    private void SeedWorkspace()
    {
        Metrics.Add(new("Programari azi", "12", "Confirmate si in asteptare"));
        Metrics.Add(new("Pacienti activi", "184", "Cu fise medicale"));
        Metrics.Add(new("Retete active", "31", "Monitorizate"));
        Metrics.Add(new("Analize noi", "8", "Necesita interpretare"));

        Appointments.Add(new("Ana Munteanu", "Dr. Rusu", "Cardiologie", DateTime.Today.AddHours(10).AddMinutes(30), "Confirmata", "Control", "Palpitatii si tensiune oscilanta"));
        Appointments.Add(new("Victor Ceban", "Dr. Popescu", "Neurologie", DateTime.Today.AddHours(12), "In_asteptare", "Initiala", "Migrene frecvente"));
        Appointments.Add(new("Irina Ursu", "Dr. Lupu", "Medicina interna", DateTime.Today.AddDays(1).AddHours(9), "Programata", "Telemedicina", "Rezultate analize"));

        Prescriptions.Add(new("Bisoprolol", "5 mg", "1 comprimat dimineata", "Dr. Rusu", "Activa", 65, DateTime.Today.AddDays(18)));
        Prescriptions.Add(new("Vitamina D3", "2000 UI", "10 picaturi zilnic", "Dr. Ceban", "Activa", 35, DateTime.Today.AddDays(6)));
        Prescriptions.Add(new("Ibuprofen", "400 mg", "la nevoie dupa masa", "Dr. Popescu", "Folosita", 100, DateTime.Today.AddDays(-4)));

        LabResults.Add(new("Hemoglobina", "135", "g/L", "120-160", "Normal"));
        LabResults.Add(new("Colesterol LDL", "4.1", "mmol/L", "<3.0", "Ridicat"));
        LabResults.Add(new("Glicemie", "5.2", "mmol/L", "3.9-5.5", "Normal"));

        Messages.Add(new("Dr. Rusu", "Buna ziua, analiza este in limite normale.", DateTime.Now.AddMinutes(-42), false));
        Messages.Add(new(Utilizator.NumeComplet, "Multumesc, doamna doctor.", DateTime.Now.AddMinutes(-38), true));

        Users.Add(new("Maria Chistrea", "maria@mediclin.local", "pacient", true, DateTime.Today.AddDays(-12)));
        Users.Add(new("Dr. Ana Rusu", "ana.rusu@mediclin.local", "medic", true, DateTime.Today.AddMonths(-2)));
        Users.Add(new("Ion Administrator", "admin@mediclin.local", "admin", true, DateTime.Today.AddMonths(-6)));

        DoctorApprovals.Add(new("Dr. Mihai Lupu", "Neurologie", "Diploma, certificat, drept de practica", false));
        DoctorApprovals.Add(new("Dr. Elena Ceban", "Cardiologie", "Diploma, contract colaborare", false));

        AuditLogs.Add(new(DateTime.Now.AddMinutes(-5), "Info", Utilizator.NumeComplet, "Autentificare", "Auth", "127.0.0.1"));
        AuditLogs.Add(new(DateTime.Now.AddMinutes(-18), "Avertisment", "Sistem", "Incercare parola gresita", "Auth", "127.0.0.1"));
        AuditLogs.Add(new(DateTime.Now.AddHours(-1), "Info", "Dr. Rusu", "Reteta emisa", "Retete", "127.0.0.1"));
    }

    private void AddAppointment()
    {
        var next = DateTime.Today.AddDays(2 + Appointments.Count(a => a.Reason.Contains("creata din interfata", StringComparison.OrdinalIgnoreCase))).AddHours(11);
        Appointments.Add(new(
            Utilizator.Rol == "pacient" ? Utilizator.NumeComplet : "Pacient nou",
            "Dr. Rusu",
            "Cardiologie",
            next,
            "Programata",
            "Initiala",
            "Programare creata din interfata"));
        SetStatus("Programarea noua a fost adaugata in agenda.");
    }

    private void CancelAppointment(AppointmentItem? item)
    {
        if (item is null)
        {
            return;
        }

        item.Status = "Anulata";
        SetStatus($"Programarea pentru {item.PatientName} a fost anulata.");
    }

    private void UpdateAppointmentStatus(string status)
    {
        var item = Appointments.FirstOrDefault(a => a.Status is "In_asteptare" or "Programata" or "Confirmata");
        if (item is null)
        {
            SetStatus("Nu exista programari active pentru actualizare.");
            return;
        }

        item.Status = status;
        SetStatus($"Status actualizat: {item.PatientName} este acum {status}.");
    }

    private void IssuePrescription()
    {
        var index = Prescriptions.Count(p => p.Name.StartsWith("Tratament nou", StringComparison.OrdinalIgnoreCase)) + 1;
        Prescriptions.Insert(0, new($"Tratament nou {index}", "conform consultatiei", "Schema salvata in reteta", $"Dr. {Utilizator.NumeComplet}", "Activa", 0, DateTime.Today.AddDays(30)));
        SetStatus("Reteta a fost emisa si este disponibila pentru pacient.");
    }

    private void AddUser()
    {
        var count = Users.Count + 1;
        Users.Insert(0, new($"Utilizator nou {count}", $"utilizator{count}@mediclin.local", "pacient", true, DateTime.Today));
        SetStatus("Utilizatorul nou a fost adaugat si activat.");
    }

    private void SendMessage()
    {
        Messages.Add(new(Utilizator.NumeComplet, ChatText.Trim(), DateTime.Now, true));
        ChatText = string.Empty;
        SetStatus("Mesajul a fost trimis.");
    }

    private void ToggleUser(UserAdminItem? user)
    {
        if (user is null)
        {
            return;
        }

        user.IsActive = !user.IsActive;
        SetStatus(user.IsActive ? "Utilizatorul a fost activat." : "Utilizatorul a fost dezactivat.");
    }

    private void ApproveDoctor(DoctorApprovalItem? doctor)
    {
        if (doctor is null)
        {
            return;
        }

        doctor.IsApproved = true;
        SetStatus($"{doctor.Name} a fost aprobat.");
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;
        AuditLogs.Insert(0, new(DateTime.Now, "Info", Utilizator.NumeComplet, message, "UI", "local"));
    }

    private static string DefaultSectionForRole(string rol) => rol switch
    {
        "admin" => "AdminDashboard",
        "medic" => "DoctorDashboard",
        _ => "PatientDashboard"
    };

    private void Navigate(string section)
    {
        ActiveSection = section;
        CurrentView = section switch
        {
            "DoctorDashboard" => new DashboardView(),
            "DoctorPatients" => new PatientsView(),
            "DoctorAppointments" => new AppointmentsView(),
            "DoctorEmr" => new EMRView(),
            "DoctorReports" => new ReportsView(),
            "DoctorSettings" => new SettingsView(),
            "PatientDashboard" => new PatientDashboardView(),
            "PatientAppointments" => new MyAppointmentsView(),
            "PatientEmr" => new MyEMRView(),
            "PatientPrescriptions" => new PrescriptionsView(),
            "PatientLabs" => new LabResultsView(),
            "PatientMessages" => new MessagesView(),
            "PatientSettings" => new AccountSettingsView(),
            "AdminDashboard" => new AdminDashboardView(),
            "AdminUsers" => new UsersManagementView(),
            "AdminDoctors" => new DoctorsVerificationView(),
            "AdminFinancial" => new FinancialView(),
            "AdminLogs" => new AuditLogsView(),
            "AdminSettings" => new GlobalSettingsView(),
            _ => CurrentView
        };
    }

    private void Logout()
    {
        global::Mediclin.UI.App.Services.CurrentUserId = null;
        _authService.Logout();
        var loginVm = new LoginViewModel(_authService, global::Mediclin.UI.App.Services);
        var loginWindow = new LoginWindow(loginVm);
        loginWindow.Show();
        CloseAction?.Invoke();
    }
}

public record MetricItem(string Title, string Value, string Description);

public class AppointmentItem : BaseViewModel
{
    private string _status;

    public AppointmentItem(string patientName, string doctorName, string specialty, DateTime dateTime, string status, string type, string reason)
    {
        PatientName = patientName;
        DoctorName = doctorName;
        Specialty = specialty;
        DateTime = dateTime;
        _status = status;
        Type = type;
        Reason = reason;
    }

    public string PatientName { get; }
    public string DoctorName { get; }
    public string Specialty { get; }
    public DateTime DateTime { get; }
    public string Type { get; }
    public string Reason { get; }
    public string DateLabel => DateTime.ToString("dd MMM");
    public string TimeLabel => DateTime.ToString("HH:mm");
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
}

public record PrescriptionItem(string Name, string Concentration, string Dosage, string Doctor, string Status, int Progress, DateTime ExpiresAt)
{
    public string ExpirationLabel => ExpiresAt < DateTime.Today ? "expirata" : $"expira in {(ExpiresAt.Date - DateTime.Today).Days} zile";
}

public record LabResultItem(string TestName, string Value, string Unit, string NormalRange, string Status);

public record MessageItem(string Sender, string Text, DateTime SentAt, bool IsMine)
{
    public string TimeLabel => SentAt.ToString("HH:mm");
}

public class UserAdminItem : BaseViewModel
{
    private bool _isActive;

    public UserAdminItem(string name, string email, string role, bool isActive, DateTime registeredAt)
    {
        Name = name;
        Email = email;
        Role = role;
        _isActive = isActive;
        RegisteredAt = registeredAt;
    }

    public string Name { get; }
    public string Email { get; }
    public string Role { get; }
    public DateTime RegisteredAt { get; }
    public string StatusLabel => IsActive ? "Activ" : "Inactiv";
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (SetProperty(ref _isActive, value))
            {
                OnPropertyChanged(nameof(StatusLabel));
            }
        }
    }
}

public class DoctorApprovalItem : BaseViewModel
{
    private bool _isApproved;

    public DoctorApprovalItem(string name, string specialty, string checklist, bool isApproved)
    {
        Name = name;
        Specialty = specialty;
        Checklist = checklist;
        _isApproved = isApproved;
    }

    public string Name { get; }
    public string Specialty { get; }
    public string Checklist { get; }
    public string StatusLabel => IsApproved ? "Aprobat" : "In asteptare";
    public bool IsApproved
    {
        get => _isApproved;
        set
        {
            if (SetProperty(ref _isApproved, value))
            {
                OnPropertyChanged(nameof(StatusLabel));
            }
        }
    }
}

public record AuditLogItem(DateTime Timestamp, string Severity, string User, string Action, string Module, string Ip)
{
    public string TimestampLabel => Timestamp.ToString("dd.MM.yyyy HH:mm");
}
