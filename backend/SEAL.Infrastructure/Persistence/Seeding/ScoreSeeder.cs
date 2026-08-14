using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    public class ScoreSeeder : IDataSeeder
    {
        private readonly ILogger<ScoreSeeder> _logger;

        public ScoreSeeder(ILogger<ScoreSeeder> logger)
        {
            _logger = logger;
        }

        public int Order => 10; // After SubmitResult and JudgeAssignment

        public async Task SeedAsync(DatabaseContext context)
        {
            if (!await context.Scores.AnyAsync())
            {
                var submission = await context.SubmitResults.Include(s => s.Track).ThenInclude(t => t.Template).FirstOrDefaultAsync();
                var judge = await context.Users.FirstOrDefaultAsync(u => u.Email == "judge1@example.com");

                if (submission == null || judge == null || submission.Track?.TemplateId == null)
                {
                    _logger.LogWarning("Required Submission, Judge, or Template not found for Score seeding.");
                    return;
                }

                var judgeRole = await context.EventRoles.FirstOrDefaultAsync(er => er.UserId == judge.Id && er.EventId == submission.Track.EventId);
                
                if (judgeRole == null)
                {
                    _logger.LogWarning("Judge Role not found for the event. Skipping Score seeding.");
                    return;
                }

                var templateCriterias = await context.TemplateCriterias
                    .Where(tc => tc.TemplateId == submission.Track.TemplateId)
                    .ToListAsync();

                if (!templateCriterias.Any())
                {
                    _logger.LogWarning("No Criterias found for the Template. Skipping Score seeding.");
                    return;
                }

                var score = new Score
                {
                    SubmitResultId = submission.Id,
                    EventRoleId = judgeRole.Id,
                    TotalScore = 8.5m,
                    Comment = "Very innovative solution, good technical implementation."
                };

                context.Scores.Add(score);
                await context.SaveChangesAsync();

                var scoreDetails = templateCriterias.Select(tc => new ScoreDetail
                {
                    ScoreId = score.Id,
                    CriteriaId = tc.CriteriaId,
                    TemplateId = tc.TemplateId,
                    Value = 8.0m
                }).ToList();

                context.ScoreDetails.AddRange(scoreDetails);
                await context.SaveChangesAsync();

                _logger.LogInformation("Seeded Score and {Count} ScoreDetails for Team submission by Judge {Email}", scoreDetails.Count, judge.Email);
            }
        }
    }
}
