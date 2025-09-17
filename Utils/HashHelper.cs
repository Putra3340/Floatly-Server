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
            var now = DateTimeOffset.UtcNow;
            var exp = DateTimeOffset.UtcNow.AddDays(int.Parse(GlobalConfiguration.TOKEN_EXPIRED_IN_DAYS));
            var rnd = new Random();

            string raw = string.Join("|", new[]
            {
        now.ToUnixTimeMilliseconds().ToString(),
        Guid.NewGuid().ToString("N").Substring(0, 8),
        rnd.Next(100000, 999999).ToString(),
        Guid.NewGuid().ToString("N"),
        exp.ToUnixTimeSeconds().ToString()
    });

            // Encode to Base64 for obfuscation
            var bytes = System.Text.Encoding.UTF8.GetBytes(raw);
            return Convert.ToBase64String(bytes);
        }

        public static string GenerateRandomLongToken(int sizeInBytes = 512)
        {
            var bytes = new byte[sizeInBytes];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_'); // URL-safe
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
