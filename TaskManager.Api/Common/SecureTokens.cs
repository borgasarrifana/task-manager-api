using System.Security.Cryptography;
using System.Text;

namespace TaskManager.Api.Common
{
    public static class SecureTokens
    {
        // 32 random bytes as lowercase hex — URL-safe, so it can go straight into a link
        public static string Generate() =>
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

        public static string Hash(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}