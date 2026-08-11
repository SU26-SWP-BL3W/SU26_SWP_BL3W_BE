using System.Security.Cryptography;
using System.Text;

namespace SEAL_Domain.Ultis
{
    /// <summary>
    /// Sinh mật khẩu tạm thời (ngẫu nhiên, đủ mạnh) cho tài khoản giám khảo được cấp khi xác thực email.
    /// </summary>
    public static class TempPasswordGenerator
    {
        // Bỏ các ký tự dễ nhầm lẫn: 0/O, 1/l/I
        private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

        public static string Generate(int length = 12)
        {
            if (length < 8) length = 8;
            var bytes = RandomNumberGenerator.GetBytes(length);
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(Chars[bytes[i] % Chars.Length]);
            }
            return sb.ToString();
        }
    }
}
