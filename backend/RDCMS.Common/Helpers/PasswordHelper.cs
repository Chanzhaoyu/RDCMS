namespace RDCMS.Common.Helpers;

public static class PasswordHelper
{
    private const int DefaultWorkFactor = 11;

    public static string HashPassword(string password, int workFactor = DefaultWorkFactor)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor);
    }

    public static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}