using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Entity;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    public class SchoolSeeder : IDataSeeder
    {
        private readonly ILogger<SchoolSeeder> _logger;

        public SchoolSeeder(ILogger<SchoolSeeder> logger)
        {
            _logger = logger;
        }

        public int Order => 1;

        public async Task SeedAsync(DatabaseContext context)
        {
            if (!await context.Schools.AnyAsync(s => s.SchoolName == "System"))
            {
                context.Schools.Add(new School
                {
                    SchoolName = "System",
                    Address = "System Default",
                });
                _logger.LogInformation("Seeded default School 'System'.");
            }

            if (!await context.Schools.AnyAsync(s => s.SchoolName == "FPT University"))
            {
                context.Schools.Add(new School
                {
                    SchoolName = "FPT University",
                    Address = "123 Le Loi, District 1, Ho Chi Minh City",
                });
                _logger.LogInformation("Seeded School 'FPT University'.");
            }

            await context.SaveChangesAsync();
        }
    }
}
