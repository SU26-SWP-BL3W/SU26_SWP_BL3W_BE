using System.Security.Cryptography;
using System.Text;

namespace SEAL_Domain.Ultis
{
    public static class FixedSaltPasswordHasher
    {
        private static readonly string _fixedSalt = "NDSDIo213n21JDKJSn21m3JDAk24Mfls154";
        public static string HashPassword(string password)
        {
            string saltedPassword = password + _fixedSalt;
            byte[] hashBytes = SHA512.HashData(Encoding.UTF8.GetBytes(saltedPassword));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
