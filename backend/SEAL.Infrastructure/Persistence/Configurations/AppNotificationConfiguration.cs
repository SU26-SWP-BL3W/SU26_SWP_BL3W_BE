using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class AppNotificationConfiguration : IEntityTypeConfiguration<AppNotification>
    {
        public void Configure(EntityTypeBuilder<AppNotification> builder)
        {
            builder.ToTable("AppNotifications");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Message).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.Type).IsRequired().HasMaxLength(16);
            builder.Property(x => x.LinkUrl).HasMaxLength(500);
            builder.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedTime });
        }
    }
}
