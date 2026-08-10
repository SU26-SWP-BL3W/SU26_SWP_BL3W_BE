using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    public class TeamSeeder : IDataSeeder
    {
        private readonly ILogger<TeamSeeder> _logger;

        public TeamSeeder(ILogger<TeamSeeder> logger)
        {
            _logger = logger;
        }

        public int Order => 7;

        public async Task SeedAsync(DatabaseContext context)
        {
            if (!await context.Teams.AnyAsync())
            {
                var @event = await context.Events.FirstOrDefaultAsync();
                var student1 = await context.Users.FirstOrDefaultAsync(u => u.Email == "student1@example.com");
                var student2 = await context.Users.FirstOrDefaultAsync(u => u.Email == "student2@example.com");

                if (@event == null || student1 == null || student2 == null)
                {
                    _logger.LogWarning("Required Event or Students not found. Skipping Team seeding.");
                    return;
                }

                var team = new Team
                {
                    EventId = @event.Id,
                    Name = "Innovation Squad",
                    Description = "A team dedicated to AI solutions.",
                    IsActive = true
                };

                context.Teams.Add(team);
                await context.SaveChangesAsync();

                // Assign members via EventRole
                var roles = new List<EventRole>
                {
                    new EventRole
                    {
                        UserId = student1.Id,
                        EventId = @event.Id,
                        TeamId = team.Id,
                        RoleName = EventRoleType.TeamLeader,
                        AssignedAt = DateTime.UtcNow
                    },
                    new EventRole
                    {
                        UserId = student2.Id,
                        EventId = @event.Id,
                        TeamId = team.Id,
                        RoleName = EventRoleType.TeamMember,
                        AssignedAt = DateTime.UtcNow
                    }
                };

                context.EventRoles.AddRange(roles);
                await context.SaveChangesAsync();
                _logger.LogInformation("Seeded Team '{TeamName}' with 2 members for Event: {EventName}", team.Name, @event.EventName);
            }
        }
    }
}
