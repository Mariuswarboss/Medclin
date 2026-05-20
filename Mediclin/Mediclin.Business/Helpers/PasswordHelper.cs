namespace Mediclin.Business.Helpers;

public static class PasswordHelper
{
    public static string HashPassword(string plaintext)
    {
        return BCrypt.Net.BCrypt.HashPassword(plaintext, workFactor: 12);
    }

   
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
