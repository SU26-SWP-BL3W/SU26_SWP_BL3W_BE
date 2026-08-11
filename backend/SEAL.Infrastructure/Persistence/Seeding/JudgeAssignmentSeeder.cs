using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    public class JudgeAssignmentSeeder : IDataSeeder
    {
        private readonly ILogger<JudgeAssignmentSeeder> _logger;

        public JudgeAssignmentSeeder(ILogger<JudgeAssignmentSeeder> logger)
        {
            _logger = logger;
        }

        public int Order => 8; // After TeamSeeder

        public async Task SeedAsync(DatabaseContext context)
        {
            var @event = await context.Events.FirstOrDefaultAsync();
            var judge = await context.Users.FirstOrDefaultAsync(u => u.Email == "judge1@example.com");

            if (@event == null || judge == null)
            {
                _logger.LogWarning("Required Event or Judge user not found. Skipping Judge assignment.");
                return;
            }

            if (!await context.EventRoles.AnyAsync(er => er.UserId == judge.Id && er.RoleName == EventRoleType.Judge))
            {
                var judgeRole = new EventRole
                {
                    UserId = judge.Id,
                    EventId = @event.Id,
                    RoleName = EventRoleType.Judge,
                    AssignedAt = DateTime.UtcNow,
                    Notes = "Assigned as a judge for the main tracks."
                };

                context.EventRoles.Add(judgeRole);
                await context.SaveChangesAsync();
                _logger.LogInformation("Assigned user {Email} as Judge for event {EventName}", judge.Email, @event.EventName);
            }
        }
    }
}
