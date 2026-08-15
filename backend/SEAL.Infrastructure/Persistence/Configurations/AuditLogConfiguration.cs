using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ActorUserId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Action).IsRequired().HasMaxLength(64);
            builder.Property(x => x.EntityType).IsRequired().HasMaxLength(64);
            builder.Property(x => x.EntityId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.EventId).HasMaxLength(64);
            builder.Property(x => x.Summary).HasMaxLength(500);
            builder.Property(x => x.PayloadJson).HasColumnType("text");

            builder.HasIndex(x => new { x.EventId, x.CreatedTime });
            builder.HasIndex(x => new { x.EntityType, x.EntityId });
            builder.HasIndex(x => x.Action);
        }
    }
}
