namespace Mediclin.Business.Services;

public class EmailService
{
    public Task SendReminderAsync(string email, string subject, string message)
    {
        Console.WriteLine($"[MediClin Email] Catre: {email}");
        Console.WriteLine($"[MediClin Email] Subiect: {subject}");
        Console.WriteLine($"[MediClin Email] Mesaj: {message}");
        return Task.CompletedTask;
    }
}
