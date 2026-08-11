using SEAL_Infrastructure.Persistence;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    public interface IDataSeeder
    {
        int Order { get; }
        Task SeedAsync(DatabaseContext context);
    }
}
