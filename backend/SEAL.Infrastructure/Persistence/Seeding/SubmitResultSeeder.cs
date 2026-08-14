using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Entity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    public class SubmitResultSeeder : IDataSeeder
    {
        private readonly ILogger<SubmitResultSeeder> _logger;

        public SubmitResultSeeder(ILogger<SubmitResultSeeder> logger)
        {
            _logger = logger;
        }

        public int Order => 9; // After Team and Track

        public async Task SeedAsync(DatabaseContext context)
        {
            if (!await context.SubmitResults.AnyAsync())
            {
                var team = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Innovation Squad");
                var track = await context.Tracks.FirstOrDefaultAsync(t => t.TrackName == "Technology & AI");
                var round = await context.Rounds.FirstOrDefaultAsync();

                if (team == null || track == null || round == null)
                {
                    _logger.LogWarning("Required Team, Track, or Round not found for SubmitResult seeding.");
                    return;
                }

                var submission = new SubmitResult
                {
                    TeamId = team.Id,
                    TrackId = track.Id,
                    RoundId = round.Id,
                    SubmissionUrl = "https://github.com/innovation-squad/seal-solution",
                    Description = "Our AI solution for traffic management.",
                    IsActive = true
                };

                context.SubmitResults.Add(submission);
                await context.SaveChangesAsync();
                _logger.LogInformation("Seeded SubmitResult for Team '{TeamName}' in Track '{TrackName}'", team.Name, track.TrackName);
            }
        }
    }
}
