using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IDataSeeder>>();
            var seeders = scope.ServiceProvider.GetServices<IDataSeeder>();

            int maxRetries = 10;
            int delaySeconds = 3;
            for (int i = 1; i <= maxRetries; i++)
            {
                try
                {
                    logger.LogInformation("Applying database migrations (Attempt {Attempt}/{MaxRetries})...", i, maxRetries);
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Database migrations applied successfully.");
                    break;
                }
                catch (Exception ex)
                {
                    if (i == maxRetries)
                    {
                        logger.LogError(ex, "Failed to apply database migrations after {MaxRetries} attempts.", maxRetries);
                        throw;
                    }
                    logger.LogWarning("Database migration attempt {Attempt} failed. Retrying in {Delay} seconds...", i, delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
            }

            logger.LogInformation("Starting database seeding...");

            foreach (var seeder in seeders.OrderBy(s => s.Order))
            {
                var seederName = seeder.GetType().Name;
                try
                {
                    logger.LogInformation("Running seeder: {SeederName}", seederName);
                    await seeder.SeedAsync(context);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while running seeder: {SeederName}", seederName);
                    throw;
                }
            }

            logger.LogInformation("Database seeding completed successfully.");
        }
    }
}
