using Microsoft.EntityFrameworkCore;
using SEAL_Domain.Entity;
using SEAL_Application.Interfaces;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence
{
    public class DatabaseContext : DbContext
    {
        private readonly ICurrentUserService? _currentUserService;

        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        public DatabaseContext(DbContextOptions<DatabaseContext> options, ICurrentUserService currentUserService) : base(options)
        {
            _currentUserService = currentUserService;
        }

        public DbSet<School> Schools { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRejection> UserRejections { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Round> Rounds { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<Criteria> Criterias { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<TemplateCriteria> TemplateCriterias { get; set; }
        public DbSet<EventRole> EventRoles { get; set; }
        public DbSet<Score> Scores { get; set; }
        public DbSet<ScoreDetail> ScoreDetails { get; set; }
        public DbSet<FinalResult> FinalResults { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<SubmitResult> SubmitResults { get; set; }
        public DbSet<TeamInvitation> TeamInvitations { get; set; }
        public DbSet<EventRoleInvitation> EventRoleInvitations { get; set; }
        public DbSet<Appeal> Appeals { get; set; }
        public DbSet<Prize> Prizes { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AppNotification> AppNotifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        public override int SaveChanges()
        {
            ApplyAuditInfo();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInfo();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditInfo()
        {
            var userId = _currentUserService?.UserId;
            var now = SEAL_Domain.Ultis.CoreHelper.SystemTimeNow;

            foreach (var entry in ChangeTracker.Entries<SEAL_Domain.Base.BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedTime = now;
                        entry.Entity.LastUpdatedTime = now;
                        if (string.IsNullOrEmpty(entry.Entity.CreatedBy))
                        {
                            entry.Entity.CreatedBy = userId;
                        }
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastUpdatedTime = now;
                        entry.Entity.LastUpdatedBy = userId;
                        break;
                }
            }
        }
    }
}
