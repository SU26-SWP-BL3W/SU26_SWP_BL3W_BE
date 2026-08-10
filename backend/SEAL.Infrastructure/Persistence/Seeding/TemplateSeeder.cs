using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    public class TemplateSeeder : IDataSeeder
    {
        private readonly ILogger<TemplateSeeder> _logger;

        public TemplateSeeder(ILogger<TemplateSeeder> logger)
        {
            _logger = logger;
        }

        public int Order => 5;

        public async Task SeedAsync(DatabaseContext context)
        {
            if (!await context.Templates.AnyAsync())
            {
                // Seed Criterias
                var criterias = new List<Criteria>
                {
                    new Criteria { CriteriaName = "Innovation", Description = "Level of creativity and originality.", IsActive = true },
                    new Criteria { CriteriaName = "Feasibility", Description = "Potential for real-world implementation.", IsActive = true },
                    new Criteria { CriteriaName = "Impact", Description = "Potential social or economic impact.", IsActive = true },
                    new Criteria { CriteriaName = "Presentation", Description = "Quality of the pitch and slides.", IsActive = true }
                };

                context.Criterias.AddRange(criterias);
                await context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} Criterias.", criterias.Count);

                // Seed Template
                var template = new Template
                {
                    TemplateName = "Standard Pitch Deck Evaluation",
                    Description = "Used for evaluating preliminary round pitch decks."
                };

                context.Templates.Add(template);
                await context.SaveChangesAsync();

                // Connect Template and Criteria
                var templateCriterias = criterias.Select(c => new TemplateCriteria
                {
                    TemplateId = template.Id,
                    CriteriaId = c.Id,
                    Weight = 25.0m,
                    MaxScore = 10.0m
                }).ToList();

                context.TemplateCriterias.AddRange(templateCriterias);
                await context.SaveChangesAsync();
                _logger.LogInformation("Seeded Template '{TemplateName}' with {Count} criteria.", template.TemplateName, templateCriterias.Count);
            }
        }
    }
}
