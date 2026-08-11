using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SEAL_Infrastructure.Persistence
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
    {
        public DatabaseContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<DatabaseContext>();

            // Đọc configuration từ appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Ưu tiên đọc từ section Database, nếu không có thì fallback về environment variable
            string connectionString;
            try
            {
                connectionString = DatabaseConnectionHelper.BuildConnectionString(configuration);
            }
            catch
            {
                // Fallback: đọc từ environment variable hoặc sử dụng giá trị mặc định
                connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? "Host=localhost;Port=5432;Database=;Username=postgres;Password=";
            }

            builder.UseNpgsql(connectionString);
            return new DatabaseContext(builder.Options);
        }
    }
}
