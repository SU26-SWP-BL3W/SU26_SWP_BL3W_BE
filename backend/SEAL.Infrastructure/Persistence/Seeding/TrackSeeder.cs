using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    public class TrackSeeder : IDataSeeder
    {
        private readonly ILogger<TrackSeeder> _logger;

        public TrackSeeder(ILogger<TrackSeeder> logger)
        {
            _logger = logger;
        }

        public int Order => 6;

        public async Task SeedAsync(DatabaseContext context)
        {
            if (!await context.Tracks.AnyAsync())
            {
                var preliminaryRound = await context.Rounds.FirstOrDefaultAsync(r => r.RoundName == "Preliminary Round");
                var template = await context.Templates.FirstOrDefaultAsync();

                if (preliminaryRound == null)
                {
                    _logger.LogWarning("Preliminary Round not found. Skipping Track seeding.");
                    return;
                }

                var tracks = new List<Track>
                {
                    new Track
                    {
                        EventId = preliminaryRound.EventId,
                        TemplateId = template?.Id,
                        TrackName = "Technology & AI",
                        Description = "Track for AI and high-tech software solutions."
                    },
                    new Track
                    {
                        EventId = preliminaryRound.EventId,
                        TemplateId = template?.Id,
                        TrackName = "Social Impact",
                        Description = "Track for solutions targeting social and environmental issues."
                    }
                };

                context.Tracks.AddRange(tracks);
                await context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} Tracks for Event: {EventId}", tracks.Count, preliminaryRound.EventId);
            }
        }
    }
}
