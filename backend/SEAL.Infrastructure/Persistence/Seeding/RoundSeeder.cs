using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    public class RoundSeeder : IDataSeeder
    {
        private readonly ILogger<RoundSeeder> _logger;

        public RoundSeeder(ILogger<RoundSeeder> logger)
        {
            _logger = logger;
        }

        public int Order => 4;

        public async Task SeedAsync(DatabaseContext context)
        {
            if (!await context.Rounds.AnyAsync())
            {
                var @event = await context.Events.FirstOrDefaultAsync();
                if (@event == null)
                {
                    _logger.LogWarning("No Event found to seed Rounds. Skipping.");
                    return;
                }

                var rounds = new List<Round>
                {
                    new Round
                    {
                        EventId = @event.Id,
                        RoundName = "Preliminary Round",
                        RoundNumber = 1,
                        StartDate = @event.StartDate,
                        EndDate = @event.StartDate.AddDays(14),
                        AdvancementRule = "Top 50% teams advance."
                    },
                    new Round
                    {
                        EventId = @event.Id,
                        RoundName = "Final Round",
                        RoundNumber = 2,
                        StartDate = @event.EndDate.AddDays(-7),
                        EndDate = @event.EndDate,
                        AdvancementRule = "Final pitch to judges."
                    }
                };

                context.Rounds.AddRange(rounds);
                await context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} Rounds for Event: {EventName}", rounds.Count, @event.EventName);
            }
        }
    }
}
