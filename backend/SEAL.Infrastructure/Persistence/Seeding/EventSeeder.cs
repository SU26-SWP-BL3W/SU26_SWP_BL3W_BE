using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Entity;
using System;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    public class EventSeeder : IDataSeeder
    {
        private readonly ILogger<EventSeeder> _logger;

        public EventSeeder(ILogger<EventSeeder> logger)
        {
            _logger = logger;
        }

        public int Order => 3;

        public async Task SeedAsync(DatabaseContext context)
        {
            if (!await context.Events.AnyAsync())
            {
                var sampleEvent = new Event
                {
                    EventName = "SEAL Innovation Challenge 2024",
                    Season = "Summer",
                    Year = 2024,
                    StartDate = DateTime.UtcNow.AddMonths(1),
                    EndDate = DateTime.UtcNow.AddMonths(3),
                    Description = "A national-wide innovation challenge for students.",
                    PhotoEventUrl = "https://scontent.fsgn2-6.fna.fbcdn.net/v/t39.30808-6/520173389_1144631161037963_6517684000583740196_n.jpg?stp=dst-jpg_tt6&cstp=mx2000x2000&ctp=s2000x2000&_nc_cat=110&ccb=1-7&_nc_sid=127cfc&_nc_ohc=pTxLUSbXuOAQ7kNvwETHSMd&_nc_oc=Adpk80P1UphbllJakAa3DZqW77_gVjljViQLMiIs6PRtAMtGLO2PcW4Tm6kUQivE7dnwnbHfqQlhrlh6xOGsL3Kw&_nc_zt=23&_nc_ht=scontent.fsgn2-6.fna&_nc_gid=hczUGlKLSWyTr6u37-RN2g&_nc_ss=7b2a8&oh=00_Af9Sfh1Jifx9YxH-N05h-dix4nJhaUtJ3on9vzXlNjBo5w&oe=6A4580C6"
                };

                context.Events.Add(sampleEvent);
                await context.SaveChangesAsync();
                _logger.LogInformation("Seeded sample Event: {EventName}", sampleEvent.EventName);
            }
        }
    }
}
