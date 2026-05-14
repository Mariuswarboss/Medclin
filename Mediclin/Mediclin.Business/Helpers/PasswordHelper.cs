namespace Mediclin.Business.Helpers;

public static class PasswordHelper
{
    public static string HashPassword(string plaintext)
    {
        return BCrypt.Net.BCrypt.HashPassword(plaintext, workFactor: 12);
    }

    /// <summary>
    /// Verifică parola; returnează false dacă inputul lipsește sau hash-ul nu e un bcrypt valid
    /// (altfel BCrypt poate arunca, iar utilizatorul vede NullReference/SaltParseException).
    /// </summary>
    public static bool VerifyPassword(string? plaintext, string? hash)
    {
        if (string.IsNullOrEmpty(plaintext) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(plaintext, hash);
        }
        catch
        {
            return false;
        }
    }
}
