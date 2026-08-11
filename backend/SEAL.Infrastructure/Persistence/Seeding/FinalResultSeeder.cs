using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Entity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    public class FinalResultSeeder : IDataSeeder
    {
        private readonly ILogger<FinalResultSeeder> _logger;

        public FinalResultSeeder(ILogger<FinalResultSeeder> logger)
        {
            _logger = logger;
        }

        public int Order => 11; // After Score

        public async Task SeedAsync(DatabaseContext context)
        {
            if (!await context.FinalResults.AnyAsync())
            {
                var team = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Innovation Squad");
                var round = await context.Rounds.FirstOrDefaultAsync(r => r.RoundName == "Preliminary Round");

                if (team == null || round == null)
                {
                    _logger.LogWarning("Required Team or Round not found for FinalResult seeding.");
                    return;
                }

                var finalResult = new FinalResult
                {
                    TeamId = team.Id,
                    RoundId = round.Id,
                    FinalScore = 8.5m,
                    Rank = 1,
                    IsAdvanced = true
                };

                context.FinalResults.Add(finalResult);
                await context.SaveChangesAsync();
                _logger.LogInformation("Seeded FinalResult for Team '{TeamName}' in Round '{RoundName}'", team.Name, round.RoundName);
            }
        }
    }
}
