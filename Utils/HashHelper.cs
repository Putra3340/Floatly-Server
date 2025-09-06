using Isopoh.Cryptography.Argon2;
using System;
using System.Text;
namespace Floaty_Music.Utils
{
    public static class HashHelper
    {
        public static string ComputeHash(string input)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
        public static string Argon2Hash(string input)
        {
            return Argon2.Hash(input);
        }
        public static string GenerateLoginToken()
        {
            return (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
            + "-" + Guid.NewGuid().ToString("N").Substring(0, 6)) + Guid.NewGuid() + (DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString();
        }
        public static bool TryDecodeBase64(string token, out string? email)
        {
            try
            {
                var bytes = Convert.FromBase64String(token);
                email = Encoding.UTF8.GetString(bytes);
                return true;
            }
            catch (FormatException) // invalid Base64
            {
                email = null;
                return false;
            }
        }

    }
}
