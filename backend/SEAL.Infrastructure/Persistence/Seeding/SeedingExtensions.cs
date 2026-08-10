using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    public static class SeedingExtensions
    {
        public static IServiceCollection AddDataSeeders(this IServiceCollection services)
        {
            var seederType = typeof(IDataSeeder);
            var seeders = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => seederType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var seeder in seeders)
            {
                services.AddScoped(seederType, seeder);
            }

            return services;
        }
    }
}
