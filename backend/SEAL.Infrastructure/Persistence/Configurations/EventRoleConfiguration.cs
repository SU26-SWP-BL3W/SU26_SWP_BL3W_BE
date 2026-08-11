using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEAL_Domain.Entity;

namespace SEAL_Infrastructure.Persistence.Configurations
{
    public class EventRoleConfiguration : IEntityTypeConfiguration<EventRole>
    {
        public void Configure(EntityTypeBuilder<EventRole> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.UserId);
            
            // Enum conversion
            builder.Property(x => x.RoleName)
                   .HasConversion<string>()
                   .IsRequired()
                   .HasMaxLength(50);
                   
            builder.Property(x => x.Notes).HasMaxLength(1000);

            // N EventRole - 1 User
            builder.HasOne(x => x.User)
                   .WithMany(x => x.EventRoles)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
                   
            // N EventRole - 1 Event
            builder.HasOne(x => x.Event)
                   .WithMany(x => x.EventRoles)
                   .HasForeignKey(x => x.EventId)
                   .OnDelete(DeleteBehavior.Cascade);

            // N EventRole - 1 Track
            builder.HasOne(x => x.Track)
                   .WithMany(t => t.EventRoles)
                   .HasForeignKey(x => x.TrackId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
