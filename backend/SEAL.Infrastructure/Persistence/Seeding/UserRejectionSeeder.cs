using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Entity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    public class UserRejectionSeeder : IDataSeeder
    {
        private readonly ILogger<UserRejectionSeeder> _logger;

        public UserRejectionSeeder(ILogger<UserRejectionSeeder> logger)
        {
            _logger = logger;
        }

        public int Order => 12; // Final steps

        public async Task SeedAsync(DatabaseContext context)
        {
            if (!await context.UserRejections.AnyAsync())
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "student2@example.com");
                if (user == null) return;

                var rejection = new UserRejection
                {
                    UserId = user.Id,
                    Reason = "Missing valid student ID document.",
                    RejectedBy = "System Admin"
                };

                context.UserRejections.Add(rejection);
                await context.SaveChangesAsync();
                _logger.LogInformation("Seeded a sample UserRejection for user: {Email}", user.Email);
            }
        }
    }
}
