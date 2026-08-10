using Microsoft.Extensions.Configuration;

namespace SEAL_Infrastructure.Persistence
{
    public static class DatabaseConnectionHelper
    {
        /// <summary>
        /// Build PostgreSQL connection string từ cấu hình Database section
        /// </summary>
        public static string BuildConnectionString(IConfiguration configuration)
        {
            var databaseSection = configuration.GetSection("Database");

            var ip = databaseSection["Ip"];
            var port = databaseSection["Port"];
            var user = databaseSection["User"];
            var password = databaseSection["Password"];
            var database = databaseSection["Database"];

            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port) ||
                string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(database))
            {
                throw new InvalidOperationException(
                    "Database configuration is incomplete. Please ensure all fields (Ip, Port, User, Password, Database) are set in appsettings.json");
            }

            return $"Host={ip};Port={port};Database={database};Username={user};Password={password}";
        }

        /// <summary>
        /// Build PostgreSQL connection string từ các tham số riêng lẻ
        /// </summary>
        public static string BuildConnectionString(string ip, string port, string user, string password, string database)
        {
            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port) ||
                string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(database))
            {
                throw new ArgumentException("All database connection parameters are required.");
            }

            return $"Host={ip};Port={port};Database={database};Username={user};Password={password}";
        }
    }
}

