using System.Text;

namespace ActorBackend.Utils
{
    public static class PasswordUtil
    {
        public static string DecodeBase64(string encoded)
        {
            byte[] data = Convert.FromBase64String(encoded);
            return Encoding.UTF8.GetString(data);
        }

        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hashed)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashed);
        }
    }
}
