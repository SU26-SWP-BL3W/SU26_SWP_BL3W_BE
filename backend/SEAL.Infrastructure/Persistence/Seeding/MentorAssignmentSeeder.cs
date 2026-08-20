using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    /// <summary>
    /// Gán mentor.ai@seal.edu.vn làm Cố vấn hạng mục "Phần mềm nâng cao" (demo / test FE mentor workspace).
    /// Idempotent — chỉ thêm EventRole nếu chưa có.
    /// </summary>
    public class MentorAssignmentSeeder : IDataSeeder
    {
        private const string MentorEmail = "mentor.ai@seal.edu.vn";
        private const string TargetTrackName = "Phần mềm nâng cao";

        private readonly ILogger<MentorAssignmentSeeder> _logger;

        public MentorAssignmentSeeder(ILogger<MentorAssignmentSeeder> logger)
        {
            _logger = logger;
        }

        public int Order => 9; // Sau JudgeAssignmentSeeder (8)

        public async Task SeedAsync(DatabaseContext context)
        {
            var mentor = await context.Users.FirstOrDefaultAsync(u => u.Email == MentorEmail);
            if (mentor == null)
            {
                _logger.LogWarning("User {Email} not found. Skipping mentor track assignment.", MentorEmail);
                return;
            }

            var track = await context.Tracks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TrackName == TargetTrackName);

            if (track == null)
            {
                _logger.LogWarning(
                    "Track \"{TrackName}\" not found. Run demo setup or create the track first.",
                    TargetTrackName);
                return;
            }

            var alreadyAssigned = await context.EventRoles.AnyAsync(er =>
                er.UserId == mentor.Id
                && er.EventId == track.EventId
                && er.TrackId == track.Id
                && er.RoleName == EventRoleType.Mentor);

            if (alreadyAssigned)
            {
                return;
            }

            var @event = await context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == track.EventId);
            context.EventRoles.Add(new EventRole
            {
                UserId = mentor.Id,
                EventId = track.EventId,
                TrackId = track.Id,
                RoleName = EventRoleType.Mentor,
                AssignedAt = DateTime.UtcNow,
                ExpiredAt = @event?.EndDate,
                Notes = $"Auto-assigned by seeder to track \"{TargetTrackName}\".",
            });

            await context.SaveChangesAsync();
            _logger.LogInformation(
                "Assigned {Email} as Mentor for track \"{TrackName}\" (event {EventId}).",
                MentorEmail,
                TargetTrackName,
                track.EventId);
        }
    }
}
