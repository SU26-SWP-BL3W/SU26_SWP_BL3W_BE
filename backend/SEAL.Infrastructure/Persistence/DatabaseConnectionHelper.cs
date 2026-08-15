using Microsoft.Extensions.Configuration;
using System;

namespace SEAL_Infrastructure.Persistence
{
    public static class DatabaseConnectionHelper
    {
        /// <summary>
        /// Build PostgreSQL connection string từ cấu hình Database section hoặc ConnectionStrings
        /// </summary>
        public static string BuildConnectionString(IConfiguration configuration)
        {
            // 1. Kiểm tra ConnectionStrings:DefaultConnection hoặc biến DATABASE_URL
            var defaultConn = configuration.GetConnectionString("DefaultConnection") ?? configuration["DATABASE_URL"];
            if (!string.IsNullOrWhiteSpace(defaultConn))
            {
                return defaultConn;
            }

            // 2. Đọc từ Database section
            var databaseSection = configuration.GetSection("Database");

            var ip = databaseSection["Ip"];
            var port = databaseSection["Port"] ?? "5432";
            var user = databaseSection["User"];
            var password = databaseSection["Password"];
            var database = databaseSection["Database"];
            var sslMode = databaseSection["SslMode"];

            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(user) || 
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(database))
            {
                throw new InvalidOperationException(
                    "Database configuration is incomplete. Please ensure all fields (Ip, Port, User, Password, Database) are set in appsettings.json or provide ConnectionStrings:DefaultConnection");
            }

            var connStr = $"Host={ip};Port={port};Database={database};Username={user};Password={password}";

            // Nếu host từ xa (không phải localhost) hoặc có chỉ định SslMode
            if (!string.IsNullOrEmpty(sslMode))
            {
                connStr += $";SSL Mode={sslMode};Trust Server Certificate=true";
            }
            else if (!ip.Equals("localhost", StringComparison.OrdinalIgnoreCase) && !ip.Equals("127.0.0.1") && !ip.Equals("::1"))
            {
                connStr += ";SSL Mode=Require;Trust Server Certificate=true";
            }

            return connStr;
        }

        /// <summary>
        /// Build PostgreSQL connection string từ các tham số riêng lẻ
        /// </summary>
        public static string BuildConnectionString(string ip, string port, string user, string password, string database, string? sslMode = null)
        {
            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port) ||
                string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(database))
            {
                throw new ArgumentException("All database connection parameters are required.");
            }

            var connStr = $"Host={ip};Port={port};Database={database};Username={user};Password={password}";
            if (!string.IsNullOrEmpty(sslMode))
            {
                connStr += $";SSL Mode={sslMode};Trust Server Certificate=true";
            }
            else if (!ip.Equals("localhost", StringComparison.OrdinalIgnoreCase) && !ip.Equals("127.0.0.1") && !ip.Equals("::1"))
            {
                connStr += ";SSL Mode=Require;Trust Server Certificate=true";
            }

            return connStr;
        }
    }
}
