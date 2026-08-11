using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    public class AdminSeeder : IDataSeeder
    {
        private readonly AdminSettings _adminSettings;
        private readonly ILogger<AdminSeeder> _logger;

        public AdminSeeder(IOptions<AdminSettings> adminSettings, ILogger<AdminSeeder> logger)
        {
            _adminSettings = adminSettings.Value;
            _logger = logger;
        }

        public int Order => 2;

        public async Task SeedAsync(DatabaseContext context)
        {
            if (string.IsNullOrEmpty(_adminSettings.Email))
            {
                _logger.LogWarning("Admin email is not configured. Skipping Admin seeding.");
                return;
            }

            if (!await context.Users.AnyAsync(u => u.Email == _adminSettings.Email))
            {
                var school = await context.Schools.FirstOrDefaultAsync(s => s.SchoolName == "System");
                var adminUser = new User
                {
                    Email = _adminSettings.Email,
                    FullName = _adminSettings.FullName,
                    PasswordHash = FixedSaltPasswordHasher.HashPassword(_adminSettings.Password),
                    IsAdmin = true,
                    IsApproved = true,
                    IsStudent = false,
                    SchoolId = school?.Id ?? ""
                };
                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
                _logger.LogInformation("Seeded Admin user: {Email}", _adminSettings.Email);
            }
        }
    }
}
